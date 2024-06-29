using Discord.WebSocket;
using Discord;
using RudeDiscordBot.Services;

namespace RudeDiscordBot.Handlers
{
    internal class MessageHandler
    {
        private readonly MessageService _messageService;

        public MessageHandler(MessageService messageService) {
            _messageService = messageService;
        }

        public Task HandleMessageReceived(SocketMessage arg)
        {
            Task.Run(async () =>
            {
                if (arg.Author.IsBot)
                {
                    return;
                }

                await _messageService.RespondCompletionAsync(arg.Channel);
            });

            return Task.CompletedTask;
        }

        public Task HandleChannelDestroyed(SocketChannel channel)
        {
            if (channel.GetChannelType() == ChannelType.Text)
            {
                _messageService.HandleDeleteChannel(channel.Id);
            }

            return Task.CompletedTask;
        }

        public Task HandleGuildUnavailable(SocketGuild guild)
        {
            foreach (var channel in guild.Channels)
            {
                if (channel.GetChannelType() == ChannelType.Text)
                {
                    _messageService.HandleDeleteChannel(channel.Id);
                }
            }

            return Task.CompletedTask;
        }
    }
}
