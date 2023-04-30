using MediatR;

namespace PinBot.Notifications;

public class MessageDeletedNotification : INotification
{
    public Cacheable<IMessage, ulong> DeletedMessage { get; }
    public Cacheable<IMessageChannel, ulong> Channel { get; }

    public MessageDeletedNotification(Cacheable<IMessage, ulong> deletedMessage, Cacheable<IMessageChannel, ulong> channel)
    {
        DeletedMessage = deletedMessage;
        Channel = channel;
    }
}