// ReSharper disable UnusedMember.Global
namespace AccountService.Messaging.Events
{
    /// <summary>
    /// событие успешного трансфера
    /// </summary>
    public class TransferCompletedEvent
    {
        /// <summary>
        /// Guid события
        /// </summary>
        public Guid EventId { get; set; } = Guid.NewGuid();
        /// <summary>
        /// дата события
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Guid счета отправителя
        /// </summary>
        public Guid SourceAccountId { get; set; }
        /// <summary>
        ///  Guid счета получателя
        /// </summary>
        public Guid DestinationAccountId { get; set; }
        /// <summary>
        /// сумма отправления
        /// </summary>
        public decimal Amount { get; set; }
        /// <summary>
        /// Валюта отправления
        /// </summary>
        public string Currency { get; set; } = string.Empty;
        /// <summary>
        /// Guid трансфера
        /// </summary>
        public Guid TransferId { get; set; }


        
        /// <summary>
        /// Meta для версионирования
        /// </summary>
        public MetaData Meta { get; set; } = new()
        {
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };

    }
}
