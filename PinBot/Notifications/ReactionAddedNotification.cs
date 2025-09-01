namespace PinBot.Notifications;

public class ReactionAddedNotification(Cacheable<IUserMessage, ulong> message,
    Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
{
    public Cacheable<IUserMessage, ulong> Message { get; set; } = message;
    public Cacheable<IMessageChannel, ulong> Channel { get; set; } = channel;
    public SocketReaction Reaction { get; set; } = reaction ?? throw new ArgumentNullException(nameof(reaction));
}