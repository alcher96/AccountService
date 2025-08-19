namespace AccountService.Messaging.Consumer;

/// <summary>
/// сообщения для карантина
/// </summary>
public class QuarantineMessage
{
    /// <summary>
    /// Guid события
    /// </summary>
    public Guid EventId { get; set; }
    /// <summary>
    /// причина
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    /// <summary>
    /// Содержимое сообщения
    /// </summary>
    public string OriginalMessage { get; set; } = string.Empty;
}