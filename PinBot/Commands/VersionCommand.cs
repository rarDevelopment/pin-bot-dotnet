using DiscordDotNetUtilities.Interfaces;
using PinBot.Models;

namespace PinBot.Commands
{
    public class VersionCommand(VersionSettings versionSettings, IDiscordFormatter discordFormatter)
        : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("version", "Get the current version number of the bot.")]
        public async Task VersionSlashCommand()
        {
            await RespondAsync(embed: discordFormatter.BuildRegularEmbedWithUserFooter("Bot Version",
                $"PinBot is at version **{versionSettings.VersionNumber}**",
                Context.User));
        }
    }
}
