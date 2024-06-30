using System.Configuration;

namespace RudeDiscordBot.Configuration;

public static class Config
{
    public static string DiscordBotToken { get; } = ConfigurationManager.AppSettings.Get("DiscordToken") ?? "";
    public static string OpenAiKey { get; } = ConfigurationManager.AppSettings.Get("OpenAiKey") ?? "";
    public static string SpeechToTextKey { get; } = ConfigurationManager.AppSettings.Get("SpeechToTextKey") ?? "";
    public static string SpeechToTextRegion { get; } = ConfigurationManager.AppSettings.Get("SpeechToTextRegion") ?? "centralus";
    public static string SpeechLanguageCode { get; } = ConfigurationManager.AppSettings.Get("SpeechLanguageCode") ?? "en-US";
    public static string TtsVoice { get; } = ConfigurationManager.AppSettings.Get("TtsVoice") ?? "Microsoft David Desktop";
    public static string WelcomeMessage { get; } = ConfigurationManager.AppSettings.Get("WelcomeMessage") ?? "Hello, how are you?";
}
