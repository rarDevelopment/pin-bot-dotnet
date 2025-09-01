namespace PinBot.Notifications;

public class MessageUpdatedNotification(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage,
        ISocketMessageChannel channel)
{
    public Cacheable<IMessage, ulong> OldMessage { get; } = oldMessage;
    public SocketMessage NewMessage { get; } = newMessage;
    public ISocketMessageChannel Channel { get; } = channel;
}