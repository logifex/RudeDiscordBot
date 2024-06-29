using Discord.Interactions;
using RudeDiscordBot.Enums;
using RudeDiscordBot.Services;

namespace RudeDiscordBot.Modules
{
    public class Chat : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly MessageService _messageService;

        public Chat(MessageService messageService)
        {
            _messageService = messageService;
        }

        [SlashCommand("personality", "Changes the bot personality for the channel.")]
        public async Task ChangePersonality(Personality personality)
        {
            await RespondAsync("Changing personality...");
            var response = await Context.Interaction.GetOriginalResponseAsync();
            _messageService.ChangePersonality(Context.Channel, personality, response);
        }
    }
}
