using System.Net;

namespace Tajan.Workflows.Extensions.Api.Models;

public class StandardResponse<T>
{
    public StandardResponse(T? data)
    {
        Data = data;
        StatusCode = (int)HttpStatusCode.OK;
        StatusMessage = HttpStatusCode.OK.ToString();
        IsSuccess = true;
        ResponseDate = DateTime.UtcNow;
        ResultCode = (int)HttpStatusCode.OK;
    }

    public StandardResponse(HttpStatusCode statusCode, string message, string? exception)
    {
        Data = default;
        StatusCode = (int)statusCode;
        StatusMessage = statusCode.ToString();
        IsSuccess = false;
        Exception = exception;
        Message = message;
        ResponseDate = DateTime.UtcNow;
        ResultCode = (int)statusCode;
    }

    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? Exception { get; set; }
    public string? Message { get; set; }
    public DateTime ResponseDate { get; set; }
    public int ResultCode { get; set; }
}
