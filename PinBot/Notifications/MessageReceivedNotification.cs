namespace PinBot.Notifications;

public class MessageReceivedNotification(SocketMessage message)
{
    public SocketMessage Message { get; set; } = message ?? throw new ArgumentNullException(nameof(message));
}