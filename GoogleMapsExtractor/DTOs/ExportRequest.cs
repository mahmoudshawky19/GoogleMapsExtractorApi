using Models;

namespace GoogleMapsExtractor.DTOs
{

    public class ExportRequest
    {
        public List<GoogleMapsPlace> Results { get; set; } = new();
        public string FileName { get; set; } = "google_maps_results.xlsx";
    }
}
