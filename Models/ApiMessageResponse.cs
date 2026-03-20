namespace ApplicationInsights.Models
{
    public class ApiMessageResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public object? Data { get; set; }
        public DateTime UtcTime { get; set; } = DateTime.UtcNow;
    }
}
