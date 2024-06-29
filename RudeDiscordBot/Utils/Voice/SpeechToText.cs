using Discord.Audio.Streams;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using RudeDiscordBot.Configuration;

namespace RudeDiscordBot.Utils.Voice
{
    internal class SpeechToText
    {
        private const int MaxSilenceTimes = 15;
        private const int RecognitionInterval = 100;
        private const uint SilenceFrameLength = AppConstants.AudioSampleRate / 1000 * RecognitionInterval * (AppConstants.AudioBitsPerSample / 8) * AppConstants.AudioChannels;

        private static readonly AudioStreamFormat AudioFormat = AudioStreamFormat.GetWaveFormatPCM(AppConstants.AudioSampleRate, AppConstants.AudioBitsPerSample, AppConstants.AudioChannels);
        private static readonly byte[] SilenceFrame = new byte[SilenceFrameLength];

        public delegate void RecognizedHandler(string recognizedText);
        public event RecognizedHandler? OnRecognized;

        public bool IsGettingAudio { get; private set; }

        private readonly SpeechConfig _speechConfig;

        public SpeechToText(SpeechConfig speechConfig)
        {
            _speechConfig = speechConfig;
        }

        public async Task FromRtpStreamAsync(InputStream stream, CancellationToken cts = default)
        {
            var stopRecognition = new TaskCompletionSource<int>();
            using var audioConfigStream = AudioInputStream.CreatePushStream(AudioFormat);
            using var audioConfig = AudioConfig.FromStreamInput(audioConfigStream);
            using var speechRecognizer = new SpeechRecognizer(_speechConfig, audioConfig);
            ConfigureSpeechRecognizer(speechRecognizer, stopRecognition);

            await speechRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            int silenceTimes = 0;
            bool started = false;

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var canRead = stream.TryReadFrame(cts, out var frame);

                    if (canRead)
                    {
                        started = true;
                        IsGettingAudio = true;
                        silenceTimes = 0;

                        while (canRead)
                        {
                            byte[] audioData = frame.Payload;
                            audioConfigStream.Write(audioData, audioData.Length);
                            canRead = stream.TryReadFrame(cts, out frame);
                        }
                    }
                    else if (started)
                    {
                        if (silenceTimes <= MaxSilenceTimes)
                        {
                            audioConfigStream.Write(SilenceFrame, SilenceFrame.Length);
                            silenceTimes++;
                        }
                        else
                        {
                            IsGettingAudio = false;
                        }
                    }

                    await Task.Delay(RecognitionInterval, cts).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                IsGettingAudio = false;
                await speechRecognizer.StopContinuousRecognitionAsync();
            }
        }

        private void ConfigureSpeechRecognizer(SpeechRecognizer speechRecognizer, TaskCompletionSource<int> stopRecognition)
        {
            speechRecognizer.Recognizing += (s, e) =>
            {
                Logger.Log($"RECOGNIZING: Text={e.Result.Text}");
            };

            speechRecognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech && e.Result.Text != string.Empty)
                {
                    Logger.Log($"RECOGNIZED: Text={e.Result.Text}");
                    OnRecognized?.Invoke(e.Result.Text);
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Logger.Log($"NOMATCH: Speech could not be recognized.");
                }
            };

            speechRecognizer.Canceled += (s, e) =>
            {
                Logger.Log($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Logger.Log($"CANCELED: ErrorCode={e.ErrorCode}");
                    Logger.Log($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Logger.Log($"CANCELED: Did you set the speech resource key and region values?");
                }

                stopRecognition.TrySetResult(0);
            };

            speechRecognizer.SessionStopped += (s, e) =>
            {
                Logger.Log("\n    Session stopped event.");
                stopRecognition.TrySetResult(0);
            };
        }
    }
}
