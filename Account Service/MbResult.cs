
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member избыф

namespace Account_Service
{
    public class MbResult<T>
    {
        /// <summary>
        /// Указывает, успешно ли выполнен запрос.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Результат запроса, если запрос успешен, или null, если произошла ошибка.
        /// </summary>
        public T? Value { get; set; }
        /// <summary>
        /// Сообщение об ошибке, если запрос неуспешен, или null, если запрос успешен.
        /// </summary>
        public string? MbError { get; set; }

        /// <summary>
        /// Список ошибок валидации, если запрос не прошёл валидацию, или null, если валидация успешна.
        /// </summary>
        public Dictionary<string, string[]>? ValidationErrors { get; set; }

      

        private MbResult(bool isSuccess, T value, string mbError, Dictionary<string, string[]> validationErrors)
        {
            IsSuccess = isSuccess;
            Value = value;
            MbError = mbError;
            ValidationErrors = validationErrors;
        }

        public static MbResult<T> Success(T value) => new(true, value, null!, null!);
        public static MbResult<T> Failure(string error) => new(false, default!, error, null!);
        public static MbResult<T> Failure(Dictionary<string, string[]> validationErrors) => new(false, default!, "Validation failed", validationErrors);
    }
}
