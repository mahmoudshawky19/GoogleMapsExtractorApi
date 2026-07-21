using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class GoogleMapsResult
    {
        public bool Success { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public List<GoogleMapsPlace> Results { get; set; } = new();
        public string Keyword { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsPartialResult { get; set; }
        public TimeSpan ElapsedTime { get; set; }
    }
}
