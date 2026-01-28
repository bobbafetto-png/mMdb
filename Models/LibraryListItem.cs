namespace mMdb.Models;

public class LibraryListItem
{
    public string WorkId { get; set; } = "";
    public string? ExternalId { get; set; }
    public string Title { get; set; } = "";
    public int? Year { get; set; }
    public string? Plot { get; set; }
    public string? PosterUrl { get; set; }

    public string EditionId { get; set; } = "";
    public string? Barcode { get; set; }
    public string? Label { get; set; }
    public string? Region { get; set; }
    public string? Cut { get; set; }
    public int? ReleaseYear { get; set; }

    public MediaFormat Format { get; set; } 
    public int Quantity { get; set; }
}
