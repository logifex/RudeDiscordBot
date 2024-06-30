using Discord.WebSocket;
using RudeDiscordBot.Services;
using RudeDiscordBot.Utils;

namespace RudeDiscordBot.Handlers
{
    internal class AudioHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly VoiceService _voiceService;

        public AudioHandler(DiscordSocketClient client, VoiceService voiceService)
        {
            _client = client;
            _voiceService = voiceService;
        }

        public Task HandleUserVoiceStateUpdated(SocketUser user, SocketVoiceState curVoiceState, SocketVoiceState nextVoiceState)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                    {
                        return;
                    }

                    if (curVoiceState.VoiceChannel != null && curVoiceState.VoiceChannel != nextVoiceState.VoiceChannel)
                    {
                        await HandleUserLeftVoiceChannelAsync(curVoiceState);
                    }

                    if (nextVoiceState.VoiceChannel != null && nextVoiceState.VoiceChannel != curVoiceState.VoiceChannel)
                    {
                        await HandleUserJoinedVoiceChannelAsync(nextVoiceState);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            });

            return Task.CompletedTask;
        }

        private async Task HandleUserLeftVoiceChannelAsync(SocketVoiceState curVoiceState)
        {
            var voiceChannel = curVoiceState.VoiceChannel;

            if (voiceChannel.ConnectedUsers.Count == 1 && voiceChannel.ConnectedUsers.First().Id == _client.CurrentUser.Id)
            {
                await _voiceService.LeaveChannelAsync(voiceChannel);
            }
        }

        private async Task HandleUserJoinedVoiceChannelAsync(SocketVoiceState nextVoiceState)
        {
            var voiceChannel = nextVoiceState.VoiceChannel;
            var guildId = voiceChannel.Guild.Id;

            if (voiceChannel.GetUser(_client.CurrentUser.Id) == null)
            {
                await _voiceService.JoinChannelAsync(voiceChannel);
                _voiceService.StartListeningForSpeech(guildId);
            }
        }
    }
}
