namespace AccountService.Messaging
{
    /// <summary>
    /// Таблица для InboxConsumer
    /// </summary>
    public class InboxConsumed
    {

        /// <summary>
        /// Guid сообщения
        /// </summary>
        public Guid MessageId { get; set; }
        /// <summary>
        /// Дата обработки сообщения
        /// </summary>
        public DateTime ConsumedAt { get; set; }
    }
}
