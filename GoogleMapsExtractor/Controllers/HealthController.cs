using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoogleMapsExtractor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "Google Maps Extractor API"
        });
    }
}
