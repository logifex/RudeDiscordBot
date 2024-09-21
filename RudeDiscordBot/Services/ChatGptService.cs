using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using RudeDiscordBot.Classes;
using RudeDiscordBot.Enums;
using System.Text.RegularExpressions;

namespace RudeDiscordBot.Services
{
    public class ChatGptService
    {
        private readonly OpenAIService _openAiService;
        private readonly Dictionary<Personality, List<ChatMessage>> _systemInstructions;

        public ChatGptService(OpenAIService openAiService)
        {
            _openAiService = openAiService;

            _systemInstructions = new() {
                {
                    Personality.Rude,
                    [
                        ChatMessage.FromSystem("You are a rude, grumpy, and cynical man."),
                        ChatMessage.FromSystem("When someone repeats themselves, you become very annoyed and express your frustration."),
                        ChatMessage.FromSystem("You do not try to help and often give dismissive or sarcastic responses."),
                    ]
                },
                {
                    Personality.Helpful,
                    [
                        ChatMessage.FromSystem("You are a helpful assistant who provides personalized answers to each user."),
                        ChatMessage.FromSystem("You encourage users to engage in conversations with each other."),
                        ChatMessage.FromSystem("You always maintain a positive and supportive attitude, making users feel valued."),
                    ]
                }
            };
        }

        public async Task<string> CreateCompletionTextAsync(IList<Message> messages, ulong authorId, Personality personality, IList<string>? systemInstructions)
        {
            var systemMessages = new List<ChatMessage>(_systemInstructions[personality]);

            if (systemInstructions != null)
            {
                foreach (var instruction in systemInstructions)
                {
                    systemMessages.Add(ChatMessage.FromSystem(instruction));
                }
            }

            var chatMessages = systemMessages.Concat(GetMessagesAsChatMessages(messages, authorId)).ToList();

            var completionResult = await _openAiService.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = chatMessages,
                Model = Models.Gpt_4o
            });

            if (completionResult.Successful)
            {
                var choice = completionResult.Choices.FirstOrDefault();
                var content = choice?.Message.Content;

                if (content != null)
                {
                    if (content.Length > 2000)
                    {
                        return content[..2000];
                    }

                    return content;
                }
            }

            if (completionResult.Error?.Message != null)
            {
                throw new Exception(completionResult.Error.Message);
            }
            else
            {
                throw new Exception("Response Error");
            }
        }

        private List<ChatMessage> GetMessagesAsChatMessages(IList<Message> messages, ulong authorId)
        {
            var chatMessages = new List<ChatMessage>();

            foreach (var message in messages)
            {
                if (message.AuthorId == authorId)
                {
                    chatMessages.Add(ChatMessage.FromAssistant(message.Content.Data));
                }
                else
                {
                    var messageContents = new List<MessageContent>();

                    foreach (var content in message.Contents)
                    {
                        if (content.Type == Content.ContentType.Image)
                        {
                            messageContents.Add(MessageContent.ImageUrlContent(content.Data));
                        }
                        else
                        {
                            messageContents.Add(MessageContent.TextContent(content.Data));
                        }
                    }

                    var chatMessage = ChatMessage.FromUser(messageContents);
                    if (message.AuthorName != null)
                    {
                        var name = Regex.Replace(message.AuthorName.Replace(' ', '_'), @"[^a-zA-Z0-9_-]", "");
                        if (!string.IsNullOrEmpty(name))
                        {
                            chatMessage.Name = name;
                        }
                    }
                    
                    chatMessages.Add(chatMessage);
                }
            }

            return chatMessages;
        }
    }
}
