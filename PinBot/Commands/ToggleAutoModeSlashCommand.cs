using DiscordDotNetUtilities.Interfaces;
using PinBot.BusinessLayer;

namespace PinBot.Commands;

public class ToggleAutoModeSlashCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly PinHandler _pinHandler;
    private readonly IDiscordFormatter _discordFormatter;

    public ToggleAutoModeSlashCommand(IPinBusinessLayer pinBusinessLayer,
        PinHandler pinHandler,
        IDiscordFormatter discordFormatter)
    {
        _pinBusinessLayer = pinBusinessLayer;
        _pinHandler = pinHandler;
        _discordFormatter = discordFormatter;
    }

    [SlashCommand("set-auto-mode", "Allow the users currently in the specified role to administrate the bot.")]
    public async Task CatchUpPins(
        [Summary("auto-mode-setting", "Setting for auto mode - true for ON, false for OFF")] bool onOrOff
        )
    {
        await DeferAsync();

        if (Context.User is not IGuildUser requestingUser)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbed("Invalid Action",
                    "Sorry, you need to be a valid user in a valid server to use this bot.",
                    Context.User));
            return;
        }

        if (!requestingUser.GuildPermissions.ManageMessages)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbed("Insufficient Permissions",
                    "Sorry, you must have the Manage Messages permission to pin messages.",
                    Context.User));
            return;
        }

        var didSave = await _pinBusinessLayer.SaveSettings(Context.Guild.Id.ToString(), onOrOff);
        if (didSave)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildRegularEmbed("Setting Changed",
                    $"Auto Mode is now {(onOrOff ? "ON" : "OFF")}",
                    Context.User));
            return;
        }
        else
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbed("Error Changing Settings",
                    "Could not save settings. Please contact the admin.",
                    Context.User));
            return;
        }
    }
}