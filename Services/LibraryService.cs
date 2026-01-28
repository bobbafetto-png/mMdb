using mMdb.Models;

namespace mMdb.Services;

public static class LibraryService
{
    public static FilmWork AddOrUpdate(
        LibraryFile library,
        Film film,
        MediaFormat format,
        string? barcode = null,
        string? label = null,
        string? region = null,
        string? cut = null,
        int? releaseYear = null,
        bool forceNewEdition = false)
    {
        var work = FindWork(library, film);

        if (work == null)
        {
            work = new FilmWork
            {
                Id = NewId("w_"),
                ExternalId = film.ExternalId,
                Title = film.Title,
                Year = film.Year,
                Plot = film.Plot,
                PosterUrl = film.PosterUrl
            };
            library.Works.Add(work);
        }

        // Edition
        FilmEdition? edition = null;

        if (!forceNewEdition)
        {
            if (!string.IsNullOrWhiteSpace(barcode))
                edition = work.Editions.FirstOrDefault(e => e.Barcode == barcode);
        }

        if (edition == null)
        {
            edition = new FilmEdition
            {
                Id = NewId("e_"),
                Barcode = Clean(barcode),
                Label = Clean(label),
                Region = Clean(region),
                Cut = Clean(cut),
                ReleaseYear = releaseYear
            };
            work.Editions.Add(edition);
        }

        // Copy: öka quantity om samma format redan finns i edition
        var copy = edition.Copies.FirstOrDefault(c => c.Format == format);
        if (copy == null)
        {
            edition.Copies.Add(new FilmCopy
            {
                Id = NewId("c_"),
                Format = format,
                Quantity = 1
            });
        }
        else
        {
            copy.Quantity += 1;
        }

        return work;
    }

    private static FilmWork? FindWork(LibraryFile library, Film film)
    {
        if (!string.IsNullOrWhiteSpace(film.ExternalId))
            return library.Works.FirstOrDefault(w => w.ExternalId == film.ExternalId);

        // fallback: titel + år
        var nt = Normalize(film.Title);
        return library.Works.FirstOrDefault(w =>
            Normalize(w.Title) == nt &&
            w.Year == film.Year);
    }

    private static string Normalize(string s)
        => new string((s ?? "").Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static string? Clean(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string NewId(string prefix)
        => prefix + Guid.NewGuid().ToString("N")[..8];
}
