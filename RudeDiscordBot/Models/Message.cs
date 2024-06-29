namespace RudeDiscordBot.Classes
{
    public class Content
    {
        public static Content Text(string text)
        {
            return new Content(ContentType.Text, text);
        }

        public static Content Image(string url)
        {
            return new Content(ContentType.Image, url);
        }

        public enum ContentType
        {
            Text,
            Image
        }

        public ContentType Type { get; }
        public string Data { get; }

        public Content(ContentType type, string data)
        {
            Type = type;
            Data = data;
        }
    }

    public class Message
    {
        public IList<Content> Contents { get; }
        public ulong AuthorId { get; }
        public string? AuthorName { get; }
        public Content Content { get => Contents[0]; }

        public Message(IList<Content> contents, ulong authorId, string? name = null)
        {
            Contents = contents;
            AuthorId = authorId;
            AuthorName = name;
        }

        public Message(Content content, ulong authorId, string? name = null): this([content], authorId, name) { }
    }
}
