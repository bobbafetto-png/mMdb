using System.Text.Json;
using mMdb.Models;

namespace mMdb.Services;

public static class FilmStore
{
    private const string FileName = "films.json";

    private static string FilePath =>
        Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, FileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static async Task<List<Film>> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new List<Film>();

        var json = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(json))
            return new List<Film>();

        return JsonSerializer.Deserialize<List<Film>>(json, JsonOptions) ?? new List<Film>();
    }

    public static async Task SaveAsync(IEnumerable<Film> films)
    {
        var json = JsonSerializer.Serialize(films, JsonOptions);
        await File.WriteAllTextAsync(FilePath, json).ConfigureAwait(false);
    }
}
