using Discord.Interactions;
using Discord.WebSocket;
using RudeDiscordBot.Enums;
using RudeDiscordBot.Services;

namespace RudeDiscordBot.Modules
{
    public class Voice : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly VoiceService _voiceService;

        public Voice(VoiceService voiceService)
        {
            _voiceService = voiceService;
        }

        [SlashCommand("joinvoice", "Joins the connected voice channel.")]
        public async Task JoinVoice()
        {
            var voiceChannel = ((SocketGuildUser)Context.User).VoiceChannel;

            if (voiceChannel == null)
            {
                await RespondAsync("No channel connected");

                return;
            }

            if (voiceChannel.GetUser(Context.Client.CurrentUser.Id) != null)
            {
                await RespondAsync("Already connected");

                return;
            }

            await RespondAsync("Connecting...");
            await _voiceService.JoinChannelAsync(voiceChannel);
            _voiceService.StartListeningForSpeech(Context.Guild.Id);
        }

        [SlashCommand("leavevoice", "Leaves the connected voice channel")]
        public async Task LeaveVoice()
        {
            var voiceChannel = ((SocketGuildUser)Context.User).VoiceChannel;

            if (voiceChannel == null || voiceChannel.GetUser(Context.Client.CurrentUser.Id) == null)
            {
                await RespondAsync("Not connected to voice");

                return;
            }

            await RespondAsync("Disconnecting...");
            await _voiceService.LeaveChannelAsync(voiceChannel);
        }

        [SlashCommand("voicepersonality", "Changes the bot personality for the connected voice channel.")]
        public async Task ChangePersonality(Personality personality)
        {
            var voiceChannel = ((SocketGuildUser)Context.User).VoiceChannel;

            if (voiceChannel == null || voiceChannel.GetUser(Context.Client.CurrentUser.Id) == null)
            {
                await RespondAsync("Not connected to voice");

                return;
            }

            await RespondAsync("Changing personality...");
            _voiceService.ChangePersonality(Context.Guild.Id, personality);
        }
    }
}
