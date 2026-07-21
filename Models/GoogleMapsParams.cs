namespace Models
{
    public class GoogleMapsParams
    {
        public string Keyword { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int TargetResults { get; set; } = 500;
        public bool Headless { get; set; } = false;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
