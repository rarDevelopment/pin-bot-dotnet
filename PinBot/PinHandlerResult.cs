namespace PinBot;

public class PinHandlerResult
{
    public string? ErrorMessage { get; set; }
    public bool IsSuccess { get; set; }
    public Embed? EmbedToSend { get; set; }
}