using Discord.Audio.Streams;
using Discord.Audio;
using RudeDiscordBot.Classes;
using Discord.WebSocket;
using RudeDiscordBot.Enums;
using Microsoft.CognitiveServices.Speech;
using RudeDiscordBot.Configuration;

namespace RudeDiscordBot.Utils.Voice
{
    internal class VoiceChannelSession : IDisposable
    {
        private const int ListenInterval = 500;

        public delegate Task SpeechRecognizeHandler(ulong guildId, ulong speakerId, string recognizedText);
        public event SpeechRecognizeHandler? OnSpeechRecognize;

        public ulong GuildId { get; private set; }
        public bool IsListening { get; private set; }
        public bool IsSpeaking { get => _textToSpeech.IsSpeaking; }
        public ulong CurrentSpeakerId { get; private set; }
        public Personality Personality { get; set; }
        public List<Message> Messages { get; private set; }

        private readonly DiscordSocketClient _discordClient;
        private readonly TextToSpeech _textToSpeech;
        private readonly SpeechToText _speechToText;
        private readonly IAudioClient _audioClient;
        private readonly AudioOutStream _audioStream;

        public VoiceChannelSession(IAudioClient audioClient, ulong guildId, DiscordSocketClient client, SpeechConfig speechConfig)
        {
            GuildId = guildId;
            Messages = [];
            _discordClient = client;
            _audioClient = audioClient;
            _audioClient.Disconnected += HandleDisconnected;
            _textToSpeech = new TextToSpeech();
            _speechToText = new SpeechToText(speechConfig);
            _audioStream = _audioClient.CreatePCMStream(AudioApplication.Voice);
        }

        public void Initialize()
        {
            _speechToText.OnRecognized += HandleSpeechRecognized;
            _ = SpeakAsync(Config.WelcomeMessage);
        }

        public void StartListeningForSpeech()
        {
            IsListening = true;

            Task.Run(async () =>
            {
                CancellationTokenSource? cancellationTokenSource = null;
                while (IsListening)
                {
                    if (!_speechToText.IsGettingAudio)
                    {
                        var speakingUserStream = GetSpeakingUserStream();

                        if (speakingUserStream.Key != 0 && speakingUserStream.Key != CurrentSpeakerId)
                        {
                            if (cancellationTokenSource != null)
                            {
                                await cancellationTokenSource.CancelAsync();
                                cancellationTokenSource.Dispose();
                            }

                            cancellationTokenSource = new CancellationTokenSource();
                            CurrentSpeakerId = speakingUserStream.Key;
                            _ = _speechToText.FromRtpStreamAsync((InputStream)speakingUserStream.Value, cancellationTokenSource.Token);
                        }
                    }

                    await Task.Delay(ListenInterval);
                }

                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
            });
        }

        public void StopListeningForSpeech()
        {
            IsListening = false;
        }

        public async Task SpeakAsync(string text)
        {
            if (_textToSpeech.IsSpeaking || _audioClient == null || _audioStream == null)
            {
                return;
            }

            await _textToSpeech.SpeakToStreamAsync(_audioStream, text);
            Messages.Add(new Message(Content.Text(text), _discordClient.CurrentUser.Id));
        }

        public void ResetConversation()
        {
            Messages = [];
            _ = SpeakAsync(Config.WelcomeMessage);
        }

        private KeyValuePair<ulong, AudioInStream> GetSpeakingUserStream()
        {
            var stream = _audioClient.GetStreams().FirstOrDefault(s => s.Key != _discordClient.CurrentUser.Id && s.Value.AvailableFrames > 0);

            return stream;
        }

        private void HandleSpeechRecognized(string text)
        {
            var user = _discordClient.GetUser(CurrentSpeakerId);
            Messages.Add(new Message(Content.Text(text), CurrentSpeakerId, user.GlobalName));
            OnSpeechRecognize?.Invoke(GuildId, CurrentSpeakerId, text);
        }

        private Task HandleDisconnected(Exception exception)
        {
            Dispose();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            StopListeningForSpeech();
            _speechToText.OnRecognized -= HandleSpeechRecognized;
            _audioClient.Disconnected -= HandleDisconnected;
            _audioStream.Dispose();
        }
    }
}
