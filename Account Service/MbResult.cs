using Swashbuckle.AspNetCore.SwaggerGen;

namespace Account_Service
{
    public class MbResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Value { get; set; }
        public string Error { get; set; }
        public Dictionary<string, string[]> ValidationErrors { get; set; }

        public MbResult()
        {
        }

        private MbResult(bool isSuccess, T value, string error, Dictionary<string, string[]> validationErrors)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            ValidationErrors = validationErrors;
        }

        public static MbResult<T> Success(T value) => new MbResult<T>(true, value, null, null);
        public static MbResult<T> Failure(string error) => new MbResult<T>(false, default, error, null);
        public static MbResult<T> Failure(Dictionary<string, string[]> validationErrors) => new MbResult<T>(false, default, "Validation failed", validationErrors);
    }
}
