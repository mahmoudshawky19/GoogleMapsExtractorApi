using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class GoogleReviewsParams
    {
        public List<string> Links { get; set; } = new List<string>();
        public int MaxReviewsPerPlace { get; set; } = 100;
        public bool Headless { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
