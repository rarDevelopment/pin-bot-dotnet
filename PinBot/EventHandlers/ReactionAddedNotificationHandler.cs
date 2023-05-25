using MediatR;
using PinBot.BusinessLayer;
using PinBot.Notifications;

namespace PinBot.EventHandlers;

public class ReactionAddedNotificationHandler : INotificationHandler<ReactionAddedNotification>
{
    private readonly IPinBusinessLayer _pinBusinessLayer;
    private readonly PinHandler _pinHandler;
    private readonly Emoji _pinEmoji = new("📌");
    private const int UserListLimit = 5;

    public ReactionAddedNotificationHandler(IPinBusinessLayer pinBusinessLayer, PinHandler pinHandler)
    {
        _pinBusinessLayer = pinBusinessLayer;
        _pinHandler = pinHandler;
    }
    public Task Handle(ReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            var reaction = notification.Reaction;
            var user = reaction.User.GetValueOrDefault();
            var message = await notification.Message.GetOrDownloadAsync();

            if (user is IGuildUser { IsBot: true })
            {
                return Task.CompletedTask;
            }

            if (!Equals(reaction.Emote, _pinEmoji))
            {
                return Task.CompletedTask;
            }

            if (message == null)
            {
                return Task.CompletedTask;
            }

            if (notification.Reaction.Channel is not IGuildChannel guildChannel)
            {
                return Task.CompletedTask;
            }

            var config = await _pinBusinessLayer.GetSettings(guildChannel.GuildId.ToString());
            if (config is not { PinVoteCount: > 0 })
            {
                return Task.CompletedTask;
            }

            var pinReactions = message.Reactions.FirstOrDefault(r => Equals(r.Key, _pinEmoji)).Value;
            if (pinReactions.ReactionCount != config.PinVoteCount)
            {
                return Task.CompletedTask;
            }

            var reactionUsersCollections = await message.GetReactionUsersAsync(_pinEmoji, UserListLimit)
                .ToListAsync(cancellationToken: cancellationToken);
            var reactionUserNames = reactionUsersCollections.FirstOrDefault()?.Select(r => r.Mention).ToList();

            if (reactionUserNames != null)
            {
                if (pinReactions.ReactionCount > reactionUserNames.Count)
                {
                    reactionUserNames.Add("and more");
                }
            }

            var pinningUserNames = reactionUserNames != null && reactionUserNames.Any()
                ? string.Join(", ",
                    reactionUserNames)
                : $"{pinReactions.ReactionCount} Votes";

            var pinResult = await _pinHandler.HandlePin(message,  pinningUserNames);
            if (pinResult.IsSuccess)
            {
                await message.Channel.SendMessageAsync(embed: pinResult.EmbedToSend,
                    messageReference: new MessageReference(message.Id,
                        guildChannel.Id,
                        guildChannel.Guild.Id,
                        false));
            }

            return Task.CompletedTask;
        }, cancellationToken);
        return Task.CompletedTask;
    }
}