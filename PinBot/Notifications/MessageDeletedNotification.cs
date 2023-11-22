using MediatR;

namespace PinBot.Notifications;

public class MessageDeletedNotification(Cacheable<IMessage, ulong> deletedMessage,
        Cacheable<IMessageChannel, ulong> channel)
    : INotification
{
    public Cacheable<IMessage, ulong> DeletedMessage { get; } = deletedMessage;
    public Cacheable<IMessageChannel, ulong> Channel { get; } = channel;
}