namespace mMdb.Models
{
    public class Film
    {
        public string Title { get; set; } = "";
        public int? Year { get; set; }
        public string? Plot { get; set; }
        public string? PosterUrl { get; set; }
                public string? ExternalId { get; set; }
    }
}
