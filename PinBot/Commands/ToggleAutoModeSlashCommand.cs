using DiscordDotNetUtilities.Interfaces;
using PinBot.BusinessLayer;

namespace PinBot.Commands;

public class ToggleAutoModeSlashCommand(IPinBusinessLayer pinBusinessLayer, IDiscordFormatter discordFormatter)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("set-auto-mode", "Enable or disable automatic mode. If off, allowed users can reply 📌 to pin a message.")]
    public async Task SetAutoMode(
        [Summary("auto-mode-setting", "Setting for auto mode - true for ON, false for OFF")] bool onOrOff
        )
    {
        await DeferAsync();

        if (Context.User is not IGuildUser requestingUser)
        {
            await FollowupAsync(embed:
                discordFormatter.BuildErrorEmbedWithUserFooter("Invalid Action",
                    "Sorry, you need to be a valid user in a valid server to use this bot.",
                    Context.User));
            return;
        }

        if (!requestingUser.GuildPermissions.Administrator)
        {
            await FollowupAsync(embed:
                discordFormatter.BuildErrorEmbedWithUserFooter("Insufficient Permissions",
                    "Sorry, you must have the Administrator permission to toggle auto mode.",
                    Context.User));
            return;
        }

        var didSave = await pinBusinessLayer.SaveSettings(Context.Guild.Id.ToString(), onOrOff);
        if (didSave)
        {
            await FollowupAsync(embed:
                discordFormatter.BuildRegularEmbedWithUserFooter("Setting Changed",
                    $"Auto Mode is now {(onOrOff ? "ON" : "OFF")}",
                    Context.User));
        }
        else
        {
            await FollowupAsync(embed:
                discordFormatter.BuildErrorEmbedWithUserFooter("Error Changing Settings",
                    "Could not save settings. Please contact the admin.",
                    Context.User));
        }
    }
}