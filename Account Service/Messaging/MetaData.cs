namespace AccountService.Messaging
{
    /// <summary>
    /// Метаданные
    /// </summary>
    public class MetaData
    {
        /// <summary>
        /// версия
        /// </summary>
        public string Version { get; set; } = "v1"; // Изменено на "v1"
        /// <summary>
        /// источник
        /// </summary>
        public string Source { get; set; } = "account-service";
        /// <summary>
        /// Guid корреляции
        /// </summary>
        public Guid CorrelationId { get; set; }
        /// <summary>
        /// Guid события вызвавшего
        /// </summary>
        public Guid CausationId { get; set; }
    }
}
