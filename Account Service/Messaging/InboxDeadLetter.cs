// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
namespace AccountService.Messaging
{
    /// <summary>
    /// карантин входящих сообщений
    /// </summary>
    public class InboxDeadLetter
    {
        /// <summary>
        /// Guid сообщения
        /// </summary>
        public Guid MessageId { get; set; }
        /// <summary>
        /// тип события
        /// </summary>
        public string EventType { get; set; } = string.Empty;
        /// <summary>
        /// СОдержимое сообщения
        /// </summary>
        public string Payload { get; set; } = string.Empty;
        /// <summary>
        /// Дата попадания в карантин
        /// </summary>
        public DateTime FailedAt { get; set; }
        /// <summary>
        /// Причина
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
