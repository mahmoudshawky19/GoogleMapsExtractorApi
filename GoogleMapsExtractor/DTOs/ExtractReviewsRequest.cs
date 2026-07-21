using System.ComponentModel.DataAnnotations;

namespace GoogleMapsExtractor.DTOs
{
    public class ExtractReviewsRequest
    {
        [Required(ErrorMessage = "At least one link is required")]
        [MinLength(1, ErrorMessage = "At least one link is required")]
        public List<string> Links { get; set; } = new();

        [Range(1, 500, ErrorMessage = "Max reviews per place must be between 1 and 500")]
        public int MaxReviewsPerPlace { get; set; } = 100;

        public bool Headless { get; set; } = true;

        [Range(10, 120, ErrorMessage = "Timeout must be between 10 and 120 seconds")]
        public int TimeoutSeconds { get; set; } = 30;
    }
}
