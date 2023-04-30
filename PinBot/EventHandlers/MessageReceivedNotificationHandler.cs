using MediatR;
using PinBot.BusinessLayer;
using PinBot.Notifications;

namespace PinBot.EventHandlers;

public class MessageReceivedNotificationHandler : INotificationHandler<MessageReceivedNotification>
{
    private const string PinEmoji = "📌";
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly PinHandler _pinHandler;
    private readonly ILogger<DiscordBot> _logger;

    public MessageReceivedNotificationHandler(IPinBusinessLayer pinBusinessLayer, PinHandler pinHandler, ILogger<DiscordBot> logger)
    {
        _pinBusinessLayer = pinBusinessLayer;
        _pinHandler = pinHandler;
        _logger = logger;
    }

    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            if (notification.Message.Channel is not SocketTextChannel guildChannel ||
                !IsRelevantPinMessage(notification.Message) ||
                notification.Message.Reference == null)
            {
                return Task.CompletedTask;
            }

            if (!notification.Message.Reference.MessageId.IsSpecified)
            {
                _logger.LogWarning("This was a pin message but with no message reference id. This might be a cache issue.");
                return Task.CompletedTask;
            }

            var settings = await _pinBusinessLayer.GetSettings(guildChannel.Guild.Id.ToString());
            if (settings == null)
            {
                return Task.CompletedTask;
            }

            if (!settings.EnableAutoMode)
            {
                if (notification.Message.Author is not IGuildUser { GuildPermissions.ManageMessages: true } ||
                    !notification.Message.Content.Contains(PinEmoji))
                {
                    return Task.CompletedTask;
                }

                var messageToBePinned =
                    await guildChannel.GetMessageAsync(notification.Message.Reference.MessageId.Value);
                var pinResult = await _pinHandler.HandlePin(messageToBePinned,
                    notification.Message.Author.Username, notification.Message);
                if (pinResult.IsSuccess)
                {
                    if (messageToBePinned is IUserMessage messageToUnpin)
                    {
                        await messageToUnpin.UnpinAsync();
                    }
                    else
                    {
                        await notification.Message.Channel.SendMessageAsync(
                            "There was an error unpinning this message from the channel.");
                    }

                    await notification.Message.Channel.SendMessageAsync(embed: pinResult.EmbedToSend,
                        messageReference: new MessageReference(messageToBePinned.Id,
                            guildChannel.Id,
                            guildChannel.Guild.Id,
                            false));
                }
                else
                {
                    await notification.Message.Channel.SendMessageAsync("There was an error pinning this message.");
                }
                return Task.CompletedTask;
            }

            if (notification.Message.Type != MessageType.ChannelPinnedMessage)
            {
                return Task.CompletedTask;
            }

            var messageToPin =
                await guildChannel.GetMessageAsync(notification.Message.Reference.MessageId.Value);
            var pinHandlerResult = await _pinHandler.HandlePin(messageToPin,
                notification.Message.Author.Username, notification.Message);
            if (pinHandlerResult.IsSuccess)
            {
                if (messageToPin is IUserMessage messageToUnpin)
                {
                    await messageToUnpin.UnpinAsync();
                }
                else
                {
                    await notification.Message.Channel.SendMessageAsync(
                        "There was an error unpinning this message from the channel.");
                }

                await notification.Message.Channel.SendMessageAsync(embed: pinHandlerResult.EmbedToSend,
                    messageReference: new MessageReference(messageToPin.Id,
                        guildChannel.Id,
                        guildChannel.Guild.Id,
                        false));
            }
            else
            {
                await notification.Message.Channel.SendMessageAsync("There was an error pinning this message.");
            }
            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }

    private static bool IsRelevantPinMessage(IMessage notificationMessage)
    {
        return notificationMessage.Type == MessageType.ChannelPinnedMessage
               || notificationMessage.Content.Contains(PinEmoji);
    }
}