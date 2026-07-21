namespace GoogleMapsExtractor.DTOs
{
    public class StatusResponse
    {
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
