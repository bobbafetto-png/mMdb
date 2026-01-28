namespace mMdb.Models;

public class LibraryFile
{
    public int Version { get; set; } = 1;
    public List<FilmWork> Works { get; set; } = new();
}

public class FilmWork
{
    public string Id { get; set; } = "";
    public string? ExternalId { get; set; }      // t.ex. imdb:tt0133093
    public string Title { get; set; } = "";
    public int? Year { get; set; }
    public string? Plot { get; set; }
    public string? PosterUrl { get; set; }

    public List<FilmEdition> Editions { get; set; } = new();
}

public class FilmEdition
{
    public string Id { get; set; } = "";
    public string? Barcode { get; set; }         // EAN/UPC
    public string? Label { get; set; }           // Arrow, Criterion, etc
    public string? Region { get; set; }          // B/2/1 etc
    public string? Cut { get; set; }             // Theatrical/Director’s/Uncut
    public int? ReleaseYear { get; set; }

    public List<FilmCopy> Copies { get; set; } = new();
}

public class FilmCopy
{
    public string Id { get; set; } = "";
    public MediaFormat Format { get; set; }
    public int Quantity { get; set; } = 1;

    public ConditionGrade? Condition { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

public enum MediaFormat { VHS, DVD, BluRay, UHD4K, Digital, Other }
public enum ConditionGrade { Mint, VeryGood, Good, Fair, Poor }
