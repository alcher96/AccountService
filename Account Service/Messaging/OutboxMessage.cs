// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
namespace AccountService.Messaging
{
    /// <summary>
    /// таблица для OutboxMessage
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        /// Guid сообщения
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        /// <summary>
        /// типо события
        /// </summary>
        public string EventType { get; set; } = string.Empty;  
        /// <summary>
        /// содержимое сообщения
        /// </summary>
        public string Payload { get; set; } = string.Empty;  
        /// <summary>
        /// дата создания
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Дата отправки
        /// </summary>
        public DateTime? SentAt { get; set; }
        /// <summary>
        /// счетчик попыток
        /// </summary>
        public int RetryCount { get; set; } = 0;

    }
}
