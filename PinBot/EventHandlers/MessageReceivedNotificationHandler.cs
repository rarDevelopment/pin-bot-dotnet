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
    public Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            if (notification.Message.Channel is not SocketTextChannel guildChannel || notification.Message.Reference == null)
            {
                return Task.CompletedTask;
            }

            if (notification.Message.Type != MessageType.ChannelPinnedMessage)
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

                var pinWebhook = await pinBusinessLayer.GetWebhook(guildChannel.Guild.Id.ToString());
                var pinChannelMention = pinWebhook != null ? $"<#{pinWebhook.ChannelId}>" : "";

                var buttonBuilder = new ComponentBuilder()
                    .WithButton("Pin in the Pin Channel", $"pinMessage:{messageForButton.Id}:{notification.Message.Id}:{notification.Message.Author.Username}", emote: new Emoji("📌"))
                    .WithButton("Dismiss This Message", $"dismissMessage");

                await notification.Message.Channel.SendMessageAsync(
                    $"This message was pinned in this channel's pins. If you want to pin it to the server's pin channel ({pinChannelMention}) instead, click the button below.",
                    messageReference: notification.Message.Reference,
                    components: buttonBuilder.Build());

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
                    await notification.Message.Channel.SendMessageAsync("There was an error unpinning this message from the channel.");
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

    [ComponentInteraction("pinMessage:*:*:*")]
    public async Task? ManualPinButton(ulong messageToPinId, ulong systemPinMessageId, string pinUserName)
    {
        await DeferAsync(ephemeral: true);

        if (Context.User is not IGuildUser { GuildPermissions.ManageMessages: true })
        {
            await FollowupAsync(
                $"{Context.User.Mention} you do not have permission to manage messages so you cannot pin this message.", ephemeral: true);
            return;
        }

        var messageToBePinned =
            await Context.Channel.GetMessageAsync(messageToPinId);
        var messageToDelete = await Context.Channel.GetMessageAsync(systemPinMessageId);

        var pinResult = await pinHandler.HandlePin(messageToBePinned, pinUserName, messageToDelete);
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

    [ComponentInteraction("dismissMessage")]
    public async Task DismissMessageButton()
    {
        if (Context.User is not IGuildUser { GuildPermissions.ManageMessages: true })
        {
            return;
        }
        var messageWithButton = ((IComponentInteraction)Context.Interaction).Message;
        await messageWithButton.DeleteAsync();
    }
}