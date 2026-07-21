using System.ComponentModel.DataAnnotations;

namespace GoogleMapsExtractor.DTOs
{
       public class ExtractRequest
    {
        [Required(ErrorMessage = "Keyword is required")]
        [MinLength(2, ErrorMessage = "Keyword must be at least 2 characters")]
        public string Keyword { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        [Range(1, 5000, ErrorMessage = "Target results must be between 1 and 5000")]
        public int TargetResults { get; set; } = 100;

        public bool Headless { get; set; } = false;

        public int TimeoutSeconds { get; set; } = 30;
    }
}
