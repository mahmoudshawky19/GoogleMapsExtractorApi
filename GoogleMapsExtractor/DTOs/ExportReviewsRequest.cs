using Models;

namespace GoogleMapsExtractor.DTOs
{
    public class ExportReviewsRequest
    {
        public List<GoogleReview> Reviews { get; set; } = new();
        public string FileName { get; set; } = "reviews.xlsx";
    }
}
