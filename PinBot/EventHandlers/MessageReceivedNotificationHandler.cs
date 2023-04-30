using MediatR;
using PinBot.BusinessLayer;
using PinBot.Notifications;

namespace PinBot.EventHandlers;

public class MessageReceivedNotificationHandler : INotificationHandler<MessageReceivedNotification>
{
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly PinHandler _pinHandler;
    private readonly ILogger<DiscordBot> _logger;
    private const string PinMessageText = "pinned a message to this channel. See all the pins.";

    public MessageReceivedNotificationHandler(IPinBusinessLayer pinBusinessLayer, PinHandler pinHandler, ILogger<DiscordBot> logger)
    {
        _pinBusinessLayer = pinBusinessLayer;
        _pinHandler = pinHandler;
        _logger = logger;
    }

    public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Message.Channel is SocketTextChannel guildChannel &&
            notification.Message.Type == MessageType.ChannelPinnedMessage &&
            notification.Message.Reference != null)
        {
            if (!notification.Message.Reference.MessageId.IsSpecified)
            {
                _logger.LogError("This was a pin message but with no message reference id. This might be a cache issue.");
                return;
            }
            var settings = await _pinBusinessLayer.GetSettings(guildChannel.Guild.Id.ToString());
            if (settings != null)
            {
                var messageToPin = await guildChannel.GetMessageAsync(notification.Message.Reference.MessageId.Value);
                var pinHandlerResult = await _pinHandler.HandlePin(messageToPin, notification.Message.Author.Username, notification.Message);
                if (pinHandlerResult.IsSuccess)
                {
                    if (messageToPin is IUserMessage messageToUnpin)
                    {
                        await messageToUnpin.UnpinAsync();
                    }
                    else
                    {
                        await notification.Message.Channel.SendMessageAsync("There was an error unpinning this message from the channel.");
                    }
                    await notification.Message.Channel.SendMessageAsync(embed: pinHandlerResult.EmbedToSend);
                }
                else
                {
                    await notification.Message.Channel.SendMessageAsync("There was an error pinning this message.");
                }
            }
        }
    }
}