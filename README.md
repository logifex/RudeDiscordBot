# RudeDiscordBot

RudeDiscordBot is a Discord bot written in C# using Discord.Net, designed to engage users in both text and voice channels with a default rude personality. It leverages ChatGPT for conversational capabilities and Microsoft Azure Speech services for voice interactions.

## Features

- **Text Channel Interactions**: Communicate with the bot in text channels where it responds using a rude personality by default.
- **Voice Channel Interactions**: 
  - Automatically joins voice channels when a user connects.
  - Uses Microsoft Azure Speech-to-Text for listening and Windows TTS for responses.
  - Automatically leaves when no users remain in the channel.
- **Image and Embed Recognition**: The bot can process images and embeds from discussions.
- **Personality Customization**: 
  - Use `/personality [personality]` to switch between 'Rude' and 'Helpful' in text channels.
  - Use `/voicepersonality [personality]` for voice channels. 
  - Personality changes reset the conversation context.
- **Voice Commands**:
  - `/joinvoice` to make the bot join your current voice channel.
  - `/leavevoice` to make the bot leave the voice channel.

## Requirements

- **Discord Bot Token**: Requires a bot token with MESSAGE CONTENT INTENT enabled.
- **OpenAI API Key**: For conversation capabilities.
- **Azure API Key**: For speech-to-text functionality. Note: Azure's free tier allows only one active speech-to-text instance at a time.
- **Windows Only**: Utilizes Windows TTS, so the chosen voice must be installed on your PC.

## Configuration

Use the `App.config` file to set the following:

- `DiscordToken`: Your Discord bot token.
- `OpenAiKey`: Your OpenAI API key.
- `SpeechToTextKey`: Your Azure subscription Speech-to-Text API key.
- `SpeechToTextRegion`: The Azure region for your Speech-to-Text service.
- `SpeechLanguageCode`: Language code for speech recognition (e.g., en-US).
- `TtsVoice`: The Windows system TTS voice name.

## Setup

1. Ensure all dependencies and required tokens are set in `App.config`.
2. Download the required native libraries (`libsodium` and `opus`):
   - After building your bot, place these libraries in the runtime directory where your bot executable runs. Precompiled binaries for Windows are available [here](https://github.com/discord-net/Discord.Net/tree/dev/voice-natives).
   - Rename `libopus.dll` to `opus.dll`.
3. Run the bot and allow up to an hour for Discord command registration.
