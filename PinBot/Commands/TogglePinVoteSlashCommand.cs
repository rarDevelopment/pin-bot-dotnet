using DiscordDotNetUtilities.Interfaces;
using PinBot.BusinessLayer;

namespace PinBot.Commands;

public class TogglePinVoteSlashCommand(IPinBusinessLayer pinBusinessLayer, IDiscordFormatter discordFormatter)
    : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("set-pin-voting", "Set the number of votes for a message to be pinned. React 📌 to vote.")]
    public async Task TogglePinVote(
        [Summary("pin-voting-count", "Setting the required number of votes for a post to be pinned. Set to 0 to disable pin voting.")] int voteCount
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
                    "Sorry, you must have the Administrator permission to toggle pin voting mode.",
                    Context.User));
            return;
        }

        var didSave = await pinBusinessLayer.SetPinVoteCount(Context.Guild.Id.ToString(), voteCount);
        if (didSave)
        {
            await FollowupAsync(embed:
                discordFormatter.BuildRegularEmbedWithUserFooter("Setting Changed",
                    $"Pin Voting is now {(voteCount > 0 ? "ON and set to require " + voteCount + " votes" : "OFF")}",
                    Context.User));
        }
        else
        {
            await FollowupAsync(embed:
                discordFormatter.BuildErrorEmbedWithUserFooter("Error Changing Settings",
                    "Could not save this setting. Please contact the admin.",
                    Context.User));
        }
    }
}