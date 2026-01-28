using System.Text.Json;
using mMdb.Models;

namespace mMdb.Services;

public static class LibraryStore
{
    private const string LibraryFileName = "library.json";
    private const string OldFilmsFileName = "films.json";

    private static string LibraryPath =>
        Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, LibraryFileName);

    private static string OldFilmsPath =>
        Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, OldFilmsFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static async Task<LibraryFile> LoadAsync()
    {
        // 1) Om library.json finns – använd den
        if (File.Exists(LibraryPath))
        {
            var json = await File.ReadAllTextAsync(LibraryPath).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(json)) return new LibraryFile();
            return JsonSerializer.Deserialize<LibraryFile>(json, JsonOptions) ?? new LibraryFile();
        }

        // 2) Annars: migrera från gamla films.json (om den finns)
        if (File.Exists(OldFilmsPath))
        {
            var oldJson = await File.ReadAllTextAsync(OldFilmsPath).ConfigureAwait(false);
            var oldFilms = string.IsNullOrWhiteSpace(oldJson)
                ? new List<Film>()
                : (JsonSerializer.Deserialize<List<Film>>(oldJson, JsonOptions) ?? new List<Film>());

            var migrated = MigrateFromOldFilms(oldFilms);
            await SaveAsync(migrated).ConfigureAwait(false);
            return migrated;
        }

        // 3) Ingen data alls
        return new LibraryFile();
    }

    public static async Task SaveAsync(LibraryFile library)
    {
        var json = JsonSerializer.Serialize(library, JsonOptions);
        await File.WriteAllTextAsync(LibraryPath, json).ConfigureAwait(false);
    }

    private static LibraryFile MigrateFromOldFilms(List<Film> oldFilms)
    {
        var lib = new LibraryFile();

        foreach (var f in oldFilms)
        {
            var work = new FilmWork
            {
                Id = NewId("w_"),
                ExternalId = f.ExternalId,
                Title = f.Title,
                Year = f.Year,
                Plot = f.Plot,
                PosterUrl = f.PosterUrl,
                Editions = new List<FilmEdition>
                {
                    new FilmEdition
                    {
                        Id = NewId("e_"),
                        // okänt i gamla data:
                        Barcode = null,
                        Label = null,
                        Region = null,
                        Cut = null,
                        ReleaseYear = null,
                        Copies = new List<FilmCopy>
                        {
                            new FilmCopy
                            {
                                Id = NewId("c_"),
                                Format = MediaFormat.Other,
                                Quantity = 1
                            }
                        }
                    }
                }
            };

            lib.Works.Add(work);
        }

        return lib;
    }

    private static string NewId(string prefix)
        => prefix + Guid.NewGuid().ToString("N")[..8];
}
