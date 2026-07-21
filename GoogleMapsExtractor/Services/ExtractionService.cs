using Models;
using OfficeOpenXml;
using Tools;

namespace GoogleMapsExtractor.Services
{
    public class ExtractionService : IExtractionService
    {
        private readonly ILogger<ExtractionService> _logger;
        private readonly ExtractGoogleMapsTool _mapsTool;
        private readonly ExtractGoogleReviewsTool _reviewsTool;

        public ExtractionService(
            ILogger<ExtractionService> logger,
            ExtractGoogleMapsTool mapsTool,
            ExtractGoogleReviewsTool reviewsTool)
        {
            _logger = logger;
            _mapsTool = mapsTool;
            _reviewsTool = reviewsTool;
        }

        public async Task<GoogleMapsResult> ExtractGoogleMapsAsync(
            GoogleMapsParams parameters,
            CancellationToken cancellationToken = default,
            IProgress<(int current, int total, string message)>? progress = null)
        {
            _logger.LogInformation("Maps extraction request: {Keyword}", parameters.Keyword);
            return await _mapsTool.ExecuteAsync(parameters, cancellationToken, progress);
        }

        public async Task<GoogleReviewsResult> ExtractGoogleReviewsAsync(
            GoogleReviewsParams parameters,
            CancellationToken cancellationToken = default,
            IProgress<(int current, int total, string message)>? progress = null)
        {
            _logger.LogInformation("Reviews extraction request: {Count} places", parameters.Links.Count);
            return await _reviewsTool.ExecuteAsync(parameters, cancellationToken, progress);
        }

        public async Task<byte[]> ExportToExcelAsync(List<GoogleMapsPlace> results)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Google Maps Results");

                string[] headers = { "Name", "Maps URL", "Website", "Phone", "Category", "Rating", "Reviews", "Address", "Extracted At" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                for (int i = 0; i < results.Count; i++)
                {
                    var place = results[i];
                    worksheet.Cells[i + 2, 1].Value = place.Name;
                    worksheet.Cells[i + 2, 2].Value = place.MapsUrl;
                    worksheet.Cells[i + 2, 3].Value = place.Website;
                    worksheet.Cells[i + 2, 4].Value = place.Phone;
                    worksheet.Cells[i + 2, 5].Value = place.Category;
                    worksheet.Cells[i + 2, 6].Value = place.Rating;
                    worksheet.Cells[i + 2, 7].Value = place.Reviews;
                    worksheet.Cells[i + 2, 8].Value = place.Address;
                    worksheet.Cells[i + 2, 9].Value = place.ExtractedAt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            });
        }

        public async Task<byte[]> ExportReviewsToExcelAsync(List<GoogleReview> reviews)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Reviews");

                string[] headers = { "Place Name", "Place URL", "Reviewer", "Profile URL", "Stars", "Review Text", "Time", "Extracted At" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                for (int i = 0; i < reviews.Count; i++)
                {
                    var review = reviews[i];
                    worksheet.Cells[i + 2, 1].Value = review.PlaceName;
                    worksheet.Cells[i + 2, 2].Value = review.PlaceUrl;
                    worksheet.Cells[i + 2, 3].Value = review.ReviewerName;
                    worksheet.Cells[i + 2, 4].Value = review.ProfileUrl;
                    worksheet.Cells[i + 2, 5].Value = review.Stars;
                    worksheet.Cells[i + 2, 6].Value = review.Text;
                    worksheet.Cells[i + 2, 7].Value = review.Time;
                    worksheet.Cells[i + 2, 8].Value = review.ExtractedAt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            });
        }
    }
}