using Discord.Webhook;
using Discord;
using MediatR;
using PinBot.BusinessLayer;
using PinBot.Notifications;

namespace PinBot.EventHandlers;
public class MessageUpdatedNotificationHandler : INotificationHandler<MessageUpdatedNotification>
{
    private readonly DiscordSocketClient _client;
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly PinHandler _pinHandler;
    private readonly ILogger<DiscordBot> _logger;

    public MessageUpdatedNotificationHandler(DiscordSocketClient client,
        IPinBusinessLayer pinBusinessLayer,
        PinHandler pinHandler,
        ILogger<DiscordBot> logger)
    {
        _client = client;
        _pinBusinessLayer = pinBusinessLayer;
        _pinHandler = pinHandler;
        _logger = logger;
    }

    public Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            var guildChannel = (notification.NewMessage.Channel as IGuildChannel);
            var guild = guildChannel?.Guild;

            if (guildChannel?.Guild == null || guild == null)
            {
                _logger.LogError($"Could not retrieve guild for updated message");
                return Task.CompletedTask;
            }

            var guildId = guild.Id.ToString();
            var pinnedMessage = await _pinBusinessLayer.GetPinnedMessage(notification.NewMessage.Id.ToString(), guildId);
            if (pinnedMessage == null)
            {
                return Task.CompletedTask;
            }

            var pinWebhook = await _pinBusinessLayer.GetWebhook(guildId);

            if (pinWebhook == null)
            {
                _logger.LogError($"Could not retrieve webhook for guild with identifier {guildId}");
                return Task.CompletedTask;
            }

            var pinChannel = await guild.GetTextChannelAsync(Convert.ToUInt64(pinWebhook.ChannelId));

            if (pinChannel == null)
            {
                _logger.LogError($"Could not retrieve channel with identifier {guildId}");
                return Task.CompletedTask;
            }

            var messageToUpdate = await pinChannel.GetMessageAsync(Convert.ToUInt64(pinnedMessage.NewPinMessageId));

            if (messageToUpdate == null)
            {
                _logger.LogWarning($"No message found to edit with identifier {pinnedMessage.NewPinMessageId}");
                return Task.CompletedTask;
            }

            var embeds = _pinHandler.BuildPinEmbedsFromExisting(notification.NewMessage, messageToUpdate);

            var discordWebhook = await guildChannel.Guild.GetWebhookAsync(Convert.ToUInt64(pinWebhook.WebhookId));
            if (discordWebhook == null)
            {
                _logger.LogError($"Could not retrieve channel with identifier {guildId}");
                return Task.CompletedTask;
            }

            var webhookClient = new DiscordWebhookClient(discordWebhook);
            await webhookClient.ModifyMessageAsync(messageToUpdate.Id, properties => properties.Embeds = embeds.ToList());

            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}