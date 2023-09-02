using DiscordDotNetUtilities.Interfaces;
using PinBot.BusinessLayer;

namespace PinBot.Commands;

public class CatchUpSlashCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly PinHandler _pinHandler;
    private readonly IDiscordFormatter _discordFormatter;

    public CatchUpSlashCommand(IPinBusinessLayer pinBusinessLayer,
        PinHandler pinHandler,
        IDiscordFormatter discordFormatter)
    {
        _pinBusinessLayer = pinBusinessLayer;
        _pinHandler = pinHandler;
        _discordFormatter = discordFormatter;
    }

    [SlashCommand("catch-up", "Converts all pinned messages in the specified channel. This cannot be undone!.")]
    public async Task CatchUpPins(
        [Summary("channel", "The channel in which to process all of the pins")] SocketTextChannel channel
        )
    {
        await DeferAsync();

        if (Context.User is not IGuildUser requestingUser)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbedWithUserFooter("Invalid Action",
                    "Sorry, you need to be a valid user in a valid server to use this bot.",
                    Context.User));
            return;
        }

        if (!requestingUser.GuildPermissions.Administrator)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbedWithUserFooter("Insufficient Permissions",
                    "Sorry, you must have the Administrator permission to run the bulk pin catch-up command.",
                    Context.User));
            return;
        }

        var bulkPinResult = await _pinHandler.ProcessPinBacklogInChannel(channel);
        if (bulkPinResult.IsSuccess)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildRegularEmbedWithUserFooter("Catch-Up Process Complete!",
                    $"The catch-up process should now be complete! Please verify that your pins have been updated in {channel.Mention}",
                    Context.User));
            return;
        }
        else
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbedWithUserFooter("Catch-Up Process Error",
                    "Sorry, there was an error with the catch-up process.",
                    Context.User));
            return;
        }
    }
}
