using Discord;
using RudeDiscordBot.Enums;

namespace RudeDiscordBot.Classes
{
    internal class MessageChannel
    {
        public Personality Personality { get; set; }
        public IMessage? FromMessage { get; set; }

        public MessageChannel(Personality personality, IMessage? fromMessage)
        {
            Personality = personality;
            FromMessage = fromMessage;
        }
    }
}
