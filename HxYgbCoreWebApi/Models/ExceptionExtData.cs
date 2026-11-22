namespace HxYgbCoreWebApi.Models
{
    public class ExceptionExtData : Exception
    {
        public ExceptionExtEnum Type { get; set; }
        public ExceptionExtData(string message, ExceptionExtEnum? type = ExceptionExtEnum.Unknown) : base(message)
        {
            Type = type??ExceptionExtEnum.Unknown;
        }
    }
    public enum ExceptionExtEnum
    {
        Unknown = 0,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        InternalServerError = 500
    }
}
