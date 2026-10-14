using FluentValidation.Results;

namespace TicketShow.Application.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() : base("Một hoặc nhiều lỗi kiểm tra tính hợp lệ đã xảy ra.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base($"Không tìm thấy {entityName} với khóa ({key}).", 404)
    {
    }
}
