using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class GoogleReview
    {
        public string ReviewId { get; set; } = string.Empty;
        public string PlaceUrl { get; set; } = string.Empty;
        public string PlaceName { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ProfileUrl { get; set; } = string.Empty;
        public string Stars { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public DateTime ExtractedAt { get; set; } = DateTime.Now;
    }
}
