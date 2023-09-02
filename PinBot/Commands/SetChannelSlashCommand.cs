using Discord.Webhook;
using DiscordDotNetUtilities.Interfaces;
using PinBot.BusinessLayer;

namespace PinBot.Commands;

public class SetChannelSlashCommand : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly PinHandler _pinHandler;
    private readonly IDiscordFormatter _discordFormatter;

    public SetChannelSlashCommand(IPinBusinessLayer pinBusinessLayer,
        PinHandler pinHandler,
        IDiscordFormatter discordFormatter)
    {
        _pinBusinessLayer = pinBusinessLayer;
        _pinHandler = pinHandler;
        _discordFormatter = discordFormatter;
    }

    [SlashCommand("set-channel", "Sets the channel that will house pins (pins from any existing pin channel will NOT be moved).")]
    public async Task SetPinChannel(
        [Summary("channel", "The channel where pins will appear")] SocketTextChannel? channel = null
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
                    "Sorry, you must have the Administrator permission to change the pin channel.",
                    Context.User));
            return;
        }

        var pinWebhook = await _pinBusinessLayer.GetWebhook(requestingUser.Guild.Id.ToString());

        if (channel == null)
        {
            if (pinWebhook != null)
            {
                await FollowupAsync(embed: _discordFormatter.BuildRegularEmbedWithUserFooter("Current Pin Channel",
                    $"The pin channel is currently set to <#{pinWebhook.ChannelId}>", requestingUser));
                return;
            }

            await FollowupAsync(embed: _discordFormatter.BuildErrorEmbedWithUserFooter("No Channel Set",
                "Please provide a channel to set it as the pin channel.", requestingUser));
            return;
        }

        if (pinWebhook != null)
        {
            var discordWebhook = await Context.Guild.GetWebhookAsync(Convert.ToUInt64(pinWebhook.WebhookId));
            if (discordWebhook != null)
            {
                var webhookClient = new DiscordWebhookClient(discordWebhook);
                await webhookClient.DeleteWebhookAsync();
            }
        }

        if (channel is IIntegrationChannel webhookChannel)
        {
            var webhook = await webhookChannel.CreateWebhookAsync("PinBot (Webhook)", File.Open("pinbot.png", FileMode.Open));
            if (webhook != null)
            {
                var didSave = await _pinBusinessLayer.SetWebhook(webhook.Id.ToString(), Context.Guild.Id.ToString(), webhook.Token, webhookChannel.Id.ToString());
                if (didSave)
                {
                    var channelMention = (webhookChannel as SocketTextChannel)?.Mention;
                    await FollowupAsync(embed: _discordFormatter.BuildRegularEmbedWithUserFooter("Pin Channel Set",
                        $"Set {channelMention ?? $"<#{webhookChannel.Id}>"} as the pin channel", requestingUser));
                }
                else
                {
                    await FollowupAsync(embed: _discordFormatter.BuildErrorEmbedWithUserFooter("Pin Channel Not Set",
                        "There was an error setting the pin channel.", requestingUser));
                }
            }
        }
    }
}