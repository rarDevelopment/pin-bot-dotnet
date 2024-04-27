using MediatR;
using PinBot.BusinessLayer;
using PinBot.Notifications;

namespace PinBot.EventHandlers;

public class MessageReceivedNotificationHandler(
    IPinBusinessLayer pinBusinessLayer,
    PinHandler pinHandler,
    ILogger<DiscordBot> logger)
    : InteractionModuleBase<SocketInteractionContext>, INotificationHandler<MessageReceivedNotification>
{
    private const string PinEmoji = "📌";

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
                logger.LogWarning("This was a pin message but with no message reference id. This might be a cache issue.");
                return Task.CompletedTask;
            }

            var settings = await pinBusinessLayer.GetSettings(guildChannel.Guild.Id.ToString());
            if (settings == null)
            {
                return Task.CompletedTask;
            }

            if (!settings.EnableAutoMode)
            {
                var messageForButton =
                    await guildChannel.GetMessageAsync(notification.Message.Reference.MessageId.Value);
                await notification.Message.DeleteAsync();

                var buttonBuilder = new ComponentBuilder()
                    .WithButton("Pin In Channel", $"pinMessage:{messageForButton.Id}:{notification.Message.Author.Username}", emote: new Emoji("📌"));

                await notification.Message.Channel.SendMessageAsync(
                    "This message was pinned in this channel's pins. If you want to pin it to the server's pin channel instead, click the button below.",
                    messageReference: notification.Message.Reference,
                    components: buttonBuilder.Build());

                return Task.CompletedTask;
            }

            if (notification.Message.Type != MessageType.ChannelPinnedMessage)
            {
                return Task.CompletedTask;
            }

            var messageToPin =
                await guildChannel.GetMessageAsync(notification.Message.Reference.MessageId.Value);
            var pinHandlerResult = await pinHandler.HandlePin(messageToPin,
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

    [ComponentInteraction("pinMessage:*:*")]
    public async Task? ManualPinButton(ulong messageToPinId, string pinUserName)
    {
        if (Context.User is not IGuildUser { GuildPermissions.ManageMessages: true })
        {
            await Context.Channel.SendMessageAsync(
                "You did not pin this message so you are not allowed to decide to move it to the server's pin channel.");
            return;
        }

        await DeferAsync();

        var messageToBePinned =
            await Context.Channel.GetMessageAsync(messageToPinId);

        var pinResult = await pinHandler.HandlePin(messageToBePinned, pinUserName);
        if (pinResult.IsSuccess)
        {
            if (messageToBePinned is IUserMessage messageToUnpin)
            {
                await messageToUnpin.UnpinAsync();

                var messageWithButton = ((IComponentInteraction)Context.Interaction).Message;
                await messageWithButton.DeleteAsync();
            }
            else
            {
                await Context.Channel.SendMessageAsync(
                    "There was an error unpinning this message from the channel.");
            }

            await ReplyAsync(embed: pinResult.EmbedToSend,
            messageReference: new MessageReference(messageToBePinned.Id,
                Context.Channel.Id,
                (Context.Channel as IGuildChannel)!.Guild.Id,
                false));
        }
        else
        {
            await ReplyAsync("There was an error pinning this message.",
                messageReference: new MessageReference(messageToBePinned.Id,
                Context.Channel.Id,
                (Context.Channel as IGuildChannel)!.Guild.Id));
        }
    }
}