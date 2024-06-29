using Discord;
using Discord.WebSocket;
using OpenAI;
using OpenAI.Managers;
using RudeDiscordBot.Services;
using RudeDiscordBot.Handlers;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.CognitiveServices.Speech;
using RudeDiscordBot.Configuration;
using RudeDiscordBot.Utils;

namespace RudeDiscordBot;

public class Bot
{
    private readonly DiscordSocketClient _client;
    private readonly OpenAiOptions _openAiOptions;
    private readonly SpeechConfig _speechConfig;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;

    public Bot()
    {
        var config = new DiscordSocketConfig()
        {
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildVoiceStates | GatewayIntents.MessageContent
        };
        _client = new DiscordSocketClient(config);
        _openAiOptions = new OpenAiOptions() { ApiKey = Config.OpenAiKey };
        _speechConfig = SpeechConfig.FromSubscription(Config.SpeechToTextKey, Config.SpeechToTextRegion);
        _speechConfig.SpeechRecognitionLanguage = Config.SpeechLanguageCode;
        _interactionService = new InteractionService(_client.Rest);
        _services = SetupServices();
    }

    public async Task InitializeAsync()
    {
        var voiceService = _services.GetRequiredService<VoiceService>();
        var messageService = _services.GetRequiredService<MessageService>();

        var audioHandler = new AudioHandler(_client, voiceService);
        var messageHandler = new MessageHandler(messageService);

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.Log += Log;

        _client.MessageReceived += messageHandler.HandleMessageReceived;

        _client.ChannelDestroyed += messageHandler.HandleChannelDestroyed;

        _client.GuildUnavailable += messageHandler.HandleGuildUnavailable;

        _client.UserVoiceStateUpdated += audioHandler.HandleUserVoiceStateUpdated;

        _client.InteractionCreated += HandleInteractionAsync;

        _client.Ready += HandleReadyAsync;

        await _client.LoginAsync(TokenType.Bot, Config.DiscordBotToken);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task HandleReadyAsync()
    {
        await _interactionService.RegisterCommandsGloballyAsync();
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);

        await _interactionService.ExecuteCommandAsync(context, _services);
    }

    private Task Log(LogMessage arg)
    {
        const string format = "{0,-10} {1,-10}";
        Logger.Log(string.Format(format, arg.Source, arg.Message));

        return Task.CompletedTask;
    }

    private ServiceProvider SetupServices() => new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_openAiOptions)
            .AddSingleton(_speechConfig)
            .AddSingleton<OpenAIService>()
            .AddSingleton<ChatGptService>()
            .AddSingleton<VoiceService>()
            .AddSingleton<MessageService>()
            .BuildServiceProvider();
}