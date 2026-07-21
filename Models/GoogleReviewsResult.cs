using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class GoogleReviewsResult
    {
        public bool Success { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public List<GoogleReview> Results { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsPartialResult { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public int PlacesProcessed { get; set; }
        public int PlacesWithReviews { get; set; }
    }
}
