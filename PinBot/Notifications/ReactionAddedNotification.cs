using MediatR;

namespace PinBot.Notifications
{
    public class ReactionAddedNotification : INotification
    {
        public Cacheable<IUserMessage, ulong> Message { get; set; }
        public Cacheable<IMessageChannel, ulong> Channel { get; set; }
        public SocketReaction Reaction { get; set; }

        public ReactionAddedNotification(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            Message = message;
            Channel = channel;
            Reaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
        }
    }
}