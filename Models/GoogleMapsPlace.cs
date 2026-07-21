using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class GoogleMapsPlace
    {
        public string Name { get; set; } = string.Empty;
        public string MapsUrl { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Rating { get; set; } = string.Empty;
        public string Reviews { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime ExtractedAt { get; set; } = DateTime.Now;
    }
}
