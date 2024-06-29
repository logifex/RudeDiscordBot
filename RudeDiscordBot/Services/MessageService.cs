using Discord;
using Discord.Rest;
using Discord.WebSocket;
using RudeDiscordBot.Classes;
using RudeDiscordBot.Enums;
using RudeDiscordBot.Utils;

namespace RudeDiscordBot.Services
{
    public class MessageService
    {
        private readonly DiscordSocketClient _client;
        private readonly ChatGptService _chatGptService;
        private readonly Dictionary<ulong, MessageChannel> _channels;
        private readonly List<string> _systemInstructions;

        public MessageService(DiscordSocketClient client, ChatGptService chatGptService)
        {
            _client = client;
            _chatGptService = chatGptService;
            _channels = [];

            _systemInstructions = 
                [
                    "You are participating in a chat with multiple people. Be aware of the group context.",
                    "When responding to someone and using their name, tag them by using their mention.",
                    "Do not mention or talk about people who have not been mentioned or participated in the conversation.",
                    "You speak in the language that users speak to you.",
                ];
        }

        public async Task SendMessageAsync(string message, IMessageChannel channel)
        {
            await channel.SendMessageAsync(message);
        }

        public async Task RespondCompletionAsync(ISocketMessageChannel channel)
        {
            using (channel.EnterTypingState())
            {
                _channels.TryGetValue(channel.Id, out var messageChannel);
                var fromMessage = messageChannel?.FromMessage;

                var cachedMessages = fromMessage != null ? channel.GetCachedMessages(fromMessage, Direction.After) : channel.CachedMessages;
                var orderedCachedMessages = cachedMessages.OrderBy(item => item.Timestamp).ToList();

                List<Message> messages = TransformCompletionMessages(orderedCachedMessages);
                var personality = messageChannel?.Personality ?? Personality.Rude;

                var guild = ((SocketGuildChannel)channel).Guild;
                var botGuildUser = guild.GetUser(_client.CurrentUser.Id);
                string[] botDetailInstructions = [$"Your mention is {botGuildUser.Mention}. Your name is {botGuildUser.DisplayName}"];
                var fullSystemInstructions = _systemInstructions.Concat(botDetailInstructions).ToArray();

                try
                {
                    var content = await _chatGptService.CreateCompletionTextAsync(messages, _client.CurrentUser.Id, personality, fullSystemInstructions);
                    await SendMessageAsync(content, channel);
                }
                catch (Exception ex)
                {
                    await SendMessageAsync("Something happened to my brain...", channel);
                    Logger.Log(ex.Message);
                }
            }
        }

        public void ChangePersonality(ISocketMessageChannel channel, Personality personality, IMessage lastMessage)
        {
            if (_channels.TryGetValue(channel.Id, out var messageChannel))
            {
                messageChannel.Personality = personality;
                messageChannel.FromMessage = lastMessage;
            }
            else
            {
                _channels.Add(channel.Id, new MessageChannel(personality, lastMessage));
            }
        }

        public void HandleDeleteChannel(ulong channelId)
        {
            _channels.Remove(channelId);
        }

        private List<Message> TransformCompletionMessages(List<SocketMessage> cachedMessages)
        {
            List<Message> messages = [];

            foreach (var item in cachedMessages)
            {
                var user = item.Author;
                var messageContents = new List<Content>();

                if (user.Id != _client.CurrentUser.Id)
                {
                    messageContents.Add(Content.Text("To mention this user use: " + user.Mention));

                    foreach (var attachment in item.Attachments)
                    {
                        if (attachment.Width.HasValue)
                        {
                            messageContents.Add(Content.Image(attachment.Url));
                        }
                    }
                    foreach (var embed in item.Embeds)
                    {
                        if (embed.Type == EmbedType.Image)
                        {
                            messageContents.Add(Content.Image(embed.Url));
                        }
                        else
                        {
                            messageContents.Add(Content.Text(embed.ToJsonString()));
                        }
                    }
                }

                messageContents.Add(Content.Text(item.Content));
                messages.Add(new Message(messageContents, user.Id, user.GlobalName));
            }

            return messages;
        }
    }
}
