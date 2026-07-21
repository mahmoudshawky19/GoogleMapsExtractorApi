using Models;

namespace GoogleMapsExtractor.DTOs
{
    public class ExtractResponse
    {
        public bool Success { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public List<GoogleMapsPlace> Results { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsPartialResult { get; set; }
        public double ElapsedSeconds { get; set; }
    }
}
