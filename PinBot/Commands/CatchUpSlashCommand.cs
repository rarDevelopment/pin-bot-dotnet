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

    [SlashCommand("catch-up", "Allow the users currently in the specified role to administrate the bot.")]
    public async Task CatchUpPins(
        [Summary("channel", "The channel in which to process all of the pins")] SocketTextChannel channel
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

        var bulkPinResult = await _pinHandler.ProcessPinBacklogInChannel(channel);
        if (bulkPinResult.IsSuccess)
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildRegularEmbed("Catch-Up Process Complete!",
                    $"The catch-up process should now be complete! Please verify that your pins have been updated in {channel.Mention}",
                    Context.User));
            return;
        }
        else
        {
            await FollowupAsync(embed:
                _discordFormatter.BuildErrorEmbed("Catch-Up Process Error",
                    "Sorry, there was an error with the catch-up process.",
                    Context.User));
            return;
        }
    }
}