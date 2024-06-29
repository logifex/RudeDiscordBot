namespace RudeDiscordBot.Utils
{
    public class Logger
    {
        internal static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now}] {message}");
        }
    }
}
