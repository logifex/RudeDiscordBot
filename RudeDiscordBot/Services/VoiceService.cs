using Discord;
using Discord.WebSocket;
using Microsoft.CognitiveServices.Speech;
using RudeDiscordBot.Enums;
using RudeDiscordBot.Utils;
using RudeDiscordBot.Utils.Voice;
using System.Collections.Concurrent;

namespace RudeDiscordBot.Services
{
    public class VoiceService
    {
        private readonly DiscordSocketClient _client;
        private readonly ChatGptService _chatGptService;
        private readonly SpeechConfig _speechConfig;
        private readonly ConcurrentDictionary<ulong, VoiceChannelSession> _sessions;
        private readonly string[] _systemInstructions;

        public VoiceService(DiscordSocketClient client, ChatGptService chatGptService, SpeechConfig speechConfig)
        {
            _client = client;
            _chatGptService = chatGptService;
            _speechConfig = speechConfig;
            _sessions = new();

            _systemInstructions =
                [
                    "You are participating in a voice chat in a voice channel with multiple people. Be aware of the group context.",
                    "You speak in the language that users speak to you.",
                ];
        }

        public async Task JoinChannelAsync(IVoiceChannel voiceChannel)
        {
            var guildId = voiceChannel.Guild.Id;
            var audioClient = await voiceChannel.ConnectAsync();
            var session = new VoiceChannelSession(audioClient, guildId, _client, _speechConfig);
            session.Initialize();
            _sessions.AddOrUpdate(guildId, session, (key, oldValue) => session);

            audioClient.Disconnected += (exception) =>
            {
                _sessions.TryRemove(guildId, out _);

                return Task.CompletedTask;
            };
        }

        public async Task LeaveChannelAsync(IVoiceChannel voiceChannel)
        {
            await voiceChannel.DisconnectAsync();
        }

        public void StartListeningForSpeech(ulong guildId)
        {
            if (_sessions.TryGetValue(guildId, out var session))
            {
                session.OnSpeechRecognize += HandleSpeechRecognize;
                session.StartListeningForSpeech();
            }
        }

        public void ChangePersonality(ulong guildId, Personality personality)
        {
            if (_sessions.TryGetValue(guildId, out var session))
            {
                session.Personality = personality;
                session.ResetConversation();
            }
        }

        private Task HandleSpeechRecognize(ulong guildId, ulong speakerId, string recognizedText)
        {
            Task.Run(async () =>
            {
                if (!_sessions.TryGetValue(guildId, out var session) || session.IsSpeaking)
                {
                    return;
                }

                var guild = _client.GetGuild(guildId);
                var botGuildUser = guild.GetUser(_client.CurrentUser.Id);
                string[] botDetailInstructions = [$"Your name is {botGuildUser.DisplayName}"];
                var fullSystemInstructions = _systemInstructions.Concat(botDetailInstructions).ToArray();

                try
                {
                    var content = await _chatGptService.CreateCompletionTextAsync(session.Messages, _client.CurrentUser.Id, session.Personality, fullSystemInstructions);
                    await session.SpeakAsync(content);
                }
                catch (Exception ex)
                {
                    await session.SpeakAsync("Something happened to my brain...");
                    Logger.Log(ex.Message);
                }
            });

            return Task.CompletedTask;
        }
    }
}
