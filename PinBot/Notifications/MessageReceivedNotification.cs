using MediatR;

namespace PinBot.Notifications
{
    public class MessageReceivedNotification(SocketMessage message) : INotification
    {
        public SocketMessage Message { get; set; } = message ?? throw new ArgumentNullException(nameof(message));
    }
}