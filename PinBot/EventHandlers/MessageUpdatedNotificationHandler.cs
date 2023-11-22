using Discord.Webhook;
using MediatR;
using PinBot.BusinessLayer;
using PinBot.Notifications;

namespace PinBot.EventHandlers;
public class MessageUpdatedNotificationHandler(DiscordSocketClient client,
        IPinBusinessLayer pinBusinessLayer,
        PinHandler pinHandler,
        ILogger<DiscordBot> logger)
    : INotificationHandler<MessageUpdatedNotification>
{
    private readonly DiscordSocketClient _client = client;

    public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            var guildChannel = (notification.NewMessage.Channel as IGuildChannel);
            var guild = guildChannel?.Guild;

            if (guildChannel?.Guild == null || guild == null)
            {
                logger.LogError("Could not retrieve guild for updated message");
                return Task.CompletedTask;
            }

            var guildId = guild.Id.ToString();
            var pinnedMessage = await pinBusinessLayer.GetPinnedMessage(notification.NewMessage.Id.ToString(), guildId);
            if (pinnedMessage == null)
            {
                return Task.CompletedTask;
            }

            var pinWebhook = await pinBusinessLayer.GetWebhook(guildId);

            if (pinWebhook == null)
            {
                logger.LogError($"Could not retrieve webhook for guild with identifier {guildId}");
                return Task.CompletedTask;
            }

            var pinChannel = await guild.GetTextChannelAsync(Convert.ToUInt64(pinWebhook.ChannelId));

            if (pinChannel == null)
            {
                logger.LogError($"Could not retrieve channel with identifier {guildId}");
                return Task.CompletedTask;
            }

            var messageToUpdate = await pinChannel.GetMessageAsync(Convert.ToUInt64(pinnedMessage.NewPinMessageId));

            if (messageToUpdate == null)
            {
                logger.LogWarning($"No message found to edit with identifier {pinnedMessage.NewPinMessageId}");
                return Task.CompletedTask;
            }

            var embeds = pinHandler.BuildPinEmbedsFromExisting(notification.NewMessage, messageToUpdate);

            var discordWebhook = await guildChannel.Guild.GetWebhookAsync(Convert.ToUInt64(pinWebhook.WebhookId));
            if (discordWebhook == null)
            {
                logger.LogError($"Could not retrieve channel with identifier {guildId}");
                return Task.CompletedTask;
            }

            var webhookClient = new DiscordWebhookClient(discordWebhook);
            await webhookClient.ModifyMessageAsync(messageToUpdate.Id, properties => properties.Embeds = embeds.ToList());

            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}