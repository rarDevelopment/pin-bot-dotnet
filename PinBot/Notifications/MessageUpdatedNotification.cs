using MediatR;

namespace PinBot.Notifications;

public class MessageUpdatedNotification : INotification
{
    public Cacheable<IMessage, ulong> OldMessage { get; }
    public SocketMessage NewMessage { get; }
    public ISocketMessageChannel Channel { get; }

    public MessageUpdatedNotification(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
    {
        OldMessage = oldMessage;
        NewMessage = newMessage;
        Channel = channel;
    }
}