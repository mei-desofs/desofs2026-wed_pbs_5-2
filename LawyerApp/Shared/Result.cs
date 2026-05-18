namespace LawyerApp.Shared
{
    public class Result
    {
        protected Result(bool success, string error)
        {
            if (success && error != string.Empty)
                throw new InvalidOperationException();
            if (!success && error == string.Empty)
                throw new InvalidOperationException();
            Success = success;
            Error = new("500", error);
        }

        protected Result(bool success, string code, string error)
        {
            if (success && error != string.Empty)
                throw new InvalidOperationException();
            if (!success && error == string.Empty)
                throw new InvalidOperationException();
            Success = success;
            Error = new(code, error);
        }

        public bool Success { get; }
        public Error Error { get; }
        public bool IsFailure => !Success;
        public bool IsSuccess => Success;

        public static Result Fail(string message)
        {
            return new Result(false, message);
        }

        public static Result Ok()
        {
            return new Result(true, string.Empty);
        }
        public static Result Ok(string result)
        {
            return new Result(true, string.Empty);
        }

    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool isSuccess, Error error, T? value) : base(isSuccess, error.Code, error.Message)
        {
            Value = value;
        }

        // Atalho para devolver Sucesso
        public static Result<T> Success(T value) => new(true, Error.None, value);

        // Atalho para devolver Falha
        public static Result<T> Failure(int code,string message) => new(false, new Error(code.ToString(),message), default);
        public static Result<T> Failure(int code,string message, T value) => new(false, new Error(code.ToString(),message), value);
        public static Result<T> Failure(string message) => new(false, new Error("500", message), default);
        public static Result<T> Ok(T value) => new(true, Error.None, value);
    }
}
