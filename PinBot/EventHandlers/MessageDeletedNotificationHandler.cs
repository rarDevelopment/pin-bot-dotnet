using MediatR;
using PinBot.BusinessLayer;
using PinBot.Notifications;

namespace PinBot.EventHandlers;
public class MessageDeletedNotificationHandler(DiscordSocketClient client, IPinBusinessLayer pinBusinessLayer,
        ILogger<DiscordBot> logger)
    : INotificationHandler<MessageDeletedNotification>
{
    public Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            var deletedMessageId = notification.DeletedMessage.Id;

            var cachedMessage = await notification.DeletedMessage.GetOrDownloadAsync();
            if (cachedMessage is { Type: MessageType.ChannelPinnedMessage })
            {
                return Task.CompletedTask;
            }

            var guild = client.Guilds.FirstOrDefault(g => g.Channels.Any(c => c.Id == notification.Channel.Id));

            if (guild == null)
            {
                logger.LogWarning("Unable to retrieve channel of deleted message.");
                return Task.CompletedTask;
            }

            var guildId = guild.Id.ToString();

            var pinnedMessage =
                await pinBusinessLayer.GetPinnedMessage(deletedMessageId.ToString(), guildId);

            if (pinnedMessage == null)
            {
                var pin = await pinBusinessLayer.GetPinnedMessageByPinId(deletedMessageId.ToString(),
                    guildId);
                if (pin == null)
                {
                    return Task.CompletedTask;
                }

                if (pin.NewPinMessageId != deletedMessageId.ToString())
                {
                    return Task.CompletedTask;
                }

                var isDeleted = await pinBusinessLayer.DeleteByPinId(deletedMessageId.ToString(), guildId);
                if (!isDeleted)
                {
                    logger.LogError($"Failed to delete pin with identifier {deletedMessageId}");
                }
            }
            else if (pinnedMessage.MessageId == deletedMessageId.ToString())
            {
                var isDeleted =
                    await pinBusinessLayer.DeleteByMessageId(deletedMessageId.ToString(), guildId);
                if (!isDeleted)
                {
                    return Task.CompletedTask;
                }

                var channel = guild.GetTextChannel(Convert.ToUInt64(pinnedMessage.PinChannelId));
                if (channel == null)
                {
                    logger.LogError($"Could not find Pin Channel with identifier {pinnedMessage.PinChannelId}");
                    return Task.CompletedTask;
                }

                await channel.DeleteMessageAsync(Convert.ToUInt64(pinnedMessage.NewPinMessageId));
            }

            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}