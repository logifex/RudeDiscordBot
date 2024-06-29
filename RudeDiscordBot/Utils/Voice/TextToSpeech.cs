using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using RudeDiscordBot.Configuration;

namespace RudeDiscordBot.Utils.Voice
{
    internal class TextToSpeech
    {
        private const int Rate = 2;

        private static readonly SpeechAudioFormatInfo AudioFormatInfo = new(AppConstants.AudioSampleRate, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);

        public bool IsSpeaking { get; private set; }

        public async Task SpeakToStreamAsync(Stream stream, string text)
        {
            IsSpeaking = true;

            using var pcmStream = new MemoryStream();

            var synth = new SpeechSynthesizer
            {
                Rate = Rate,
            };
            synth.SelectVoice(Config.TtsVoice);
            synth.SetOutputToAudioStream(pcmStream, AudioFormatInfo);
            synth.Speak(text);
            synth.Dispose();
            pcmStream.Position = 0;

            try
            {
                await pcmStream.CopyToAsync(stream);
            }
            catch
            {
            }
            finally
            {
                await stream.FlushAsync();
                IsSpeaking = false;
            }
        }
    }
}
