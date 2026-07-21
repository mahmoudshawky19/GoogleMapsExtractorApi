using GoogleMapsExtractor.DTOs;
using GoogleMapsExtractor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace GoogleMapsExtractor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ExtractionController : ControllerBase
    {
        private readonly IExtractionService _extractionService;
        private readonly ILogger<ExtractionController> _logger;

        public ExtractionController(IExtractionService extractionService, ILogger<ExtractionController> logger)
        {
            _extractionService = extractionService;
            _logger = logger;
        }

        /// <summary>
        /// Extract data from Google Maps
        /// </summary>
        /// <param name="request">Extraction criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpPost("googlemapsCompanies")]
        [ProducesResponseType(typeof(ExtractResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExtractGoogleMapsCompanies([FromBody] ExtractRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("📥 Extraction request: {Keyword}, {Location}, Target: {Target}",
                    request.Keyword, request.Location, request.TargetResults);

                var parameters = new GoogleMapsParams
                {
                    Keyword = request.Keyword,
                    Location = request.Location,
                    TargetResults = request.TargetResults,
                    Headless = request.Headless,
                    TimeoutSeconds = request.TimeoutSeconds
                };

          
                var progress = new Progress<(int current, int total, string message)>();
                progress.ProgressChanged += (sender, args) =>
                {
                    _logger.LogInformation("Progress: {Current}/{Total} - {Message}",
                        args.current, args.total, args.message);
                };

                var result = await _extractionService.ExtractGoogleMapsAsync(
                    parameters,
                    cancellationToken,
                    progress);

                var response = new ExtractResponse
                {
                    Success = result.Success,
                    TaskId = result.TaskId,
                    TotalCount = result.TotalCount,
                    Results = result.Results,
                    Message = result.Message,
                    Timestamp = result.Timestamp,
                    IsPartialResult = result.IsPartialResult,
                    ElapsedSeconds = result.ElapsedTime.TotalSeconds
                };

                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return Ok(new ExtractResponse
                {
                    Success = false,
                    Message = "Operation cancelled by user",
                    Timestamp = DateTime.UtcNow,
                    IsPartialResult = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during extraction");
                return StatusCode(500, new ExtractResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        /// <summary>
        /// Extract reviews from Google Maps
        /// </summary>
        [HttpPost("extract-reviews")]
        [ProducesResponseType(typeof(GoogleReviewsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExtractReviews(
            [FromBody] ExtractReviewsRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Reviews extraction request: {Count} places",
                    request.Links?.Count ?? 0);

                if (request.Links == null || request.Links.Count == 0)
                {
                    return BadRequest(new { error = "No links provided" });
                }

                var parameters = new GoogleReviewsParams
                {
                    Links = request.Links,
                    MaxReviewsPerPlace = request.MaxReviewsPerPlace,
                    Headless = request.Headless,
                    TimeoutSeconds = request.TimeoutSeconds
                };

                var progress = new Progress<(int current, int total, string message)>();
                progress.ProgressChanged += (sender, args) =>
                {
                    _logger.LogInformation("Progress: {Current}/{Total} - {Message}",
                        args.current, args.total, args.message);
                };

                var result = await _extractionService.ExtractGoogleReviewsAsync(
                    parameters,
                    cancellationToken,
                    progress);

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return Ok(new GoogleReviewsResult
                {
                    Success = false,
                    Message = "Operation cancelled by user",
                    Timestamp = DateTime.UtcNow,
                    IsPartialResult = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reviews extraction");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Export reviews to Excel
        /// </summary>
        [HttpPost("export/reviews-excel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportReviewsToExcel([FromBody] ExportReviewsRequest request)
        {
            if (request.Reviews == null || !request.Reviews.Any())
            {
                return BadRequest("No reviews to export");
            }

            try
            {
                var bytes = await _extractionService.ExportReviewsToExcelAsync(request.Reviews);
                var fileName = string.IsNullOrEmpty(request.FileName)
                    ? $"reviews_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : request.FileName;

                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting reviews to Excel");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        /// <summary>
        /// Export results to Excel file
        /// </summary>
        [HttpPost("export/companies-excel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportCompaniesToExcel([FromBody] ExportRequest request)
        {
            if (request.Results == null || !request.Results.Any())
            {
                return BadRequest("No results to export");
            }

            try
            {
                var bytes = await _extractionService.ExportToExcelAsync(request.Results);
                var fileName = string.IsNullOrEmpty(request.FileName)
                    ? $"google_maps_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    : request.FileName;

                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Export results to CSV
        /// </summary>
        [HttpPost("export/companies-csv")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ExportCompaniesToCsv([FromBody] ExportRequest request)
        {
            if (request.Results == null || !request.Results.Any())
            {
                return BadRequest("No results to export");
            }

            try
            {
                var csv = new System.Text.StringBuilder();

                 csv.AppendLine("Name,MapsUrl,Website,Phone,Category,Rating,Reviews,Address,ExtractedAt");

                 foreach (var place in request.Results)
                {
                    csv.AppendLine($"\"{EscapeCsv(place.Name)}\"," +
                                   $"\"{place.MapsUrl}\"," +
                                   $"\"{EscapeCsv(place.Website)}\"," +
                                   $"\"{EscapeCsv(place.Phone)}\"," +
                                   $"\"{EscapeCsv(place.Category)}\"," +
                                   $"\"{EscapeCsv(place.Rating)}\"," +
                                   $"\"{EscapeCsv(place.Reviews)}\"," +
                                   $"\"{EscapeCsv(place.Address)}\"," +
                                   $"\"{place.ExtractedAt:yyyy-MM-dd HH:mm:ss}\"");
                }

                 var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

                 var bom = new byte[] { 0xEF, 0xBB, 0xBF };
                var bytesWithBom = bom.Concat(bytes).ToArray();

                var fileName = string.IsNullOrEmpty(request.FileName)
                    ? $"google_maps_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                    : request.FileName.Replace(".xlsx", ".csv");

                return File(bytesWithBom, "text/csv; charset=utf-8", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
