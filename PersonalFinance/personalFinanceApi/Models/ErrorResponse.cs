namespace PersonalFinanceApi.Models
{
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? ExceptionType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? RequestId { get; set; }
        public string? Path { get; set; }
        public int StatusCode { get; set; }
    }
}