using Models;

namespace GoogleMapsExtractor.Services
{
    public interface IExtractionService
    {
        Task<GoogleMapsResult> ExtractGoogleMapsAsync(
            GoogleMapsParams parameters,
            CancellationToken cancellationToken = default,
            IProgress<(int current, int total, string message)>? progress = null);

        Task<GoogleReviewsResult> ExtractGoogleReviewsAsync(
            GoogleReviewsParams parameters,
            CancellationToken cancellationToken = default,
            IProgress<(int current, int total, string message)>? progress = null);

        Task<byte[]> ExportToExcelAsync(List<GoogleMapsPlace> results);
        Task<byte[]> ExportReviewsToExcelAsync(List<GoogleReview> reviews);
    }
}