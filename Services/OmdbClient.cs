using System.Net.Http.Json;
using System.Text.Json.Serialization;
using mMdb.Models;

namespace mMdb.Services;

public class OmdbClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public OmdbClient(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    // 1) Söklista (många träffar)
    public async Task<List<OmdbSearchItem>> SearchAsync(string query, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<OmdbSearchItem>();

        var url = $"https://www.omdbapi.com/?s={Uri.EscapeDataString(query)}&type=movie&apikey={_apiKey}";
        var dto = await _http.GetFromJsonAsync<SearchResponse>(url);

        if (dto?.Response != "True" || dto.Search == null)
            return new List<OmdbSearchItem>();

        return dto.Search
            .Where(x => !string.IsNullOrWhiteSpace(x.imdbID))
            .Take(limit)
            .Select(x => new OmdbSearchItem
            {
                Title = x.Title ?? "",
                Year = x.Year ?? "",
                ImdbId = x.imdbID ?? "",
                Poster = x.Poster
            })
            .ToList();
    }

    // 2) Hämta full info via imdbID (bästa sättet efter val)
    public async Task<Film?> GetByImdbIdAsync(string imdbId)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
            return null;

        var url = $"https://www.omdbapi.com/?i={Uri.EscapeDataString(imdbId)}&plot=full&apikey={_apiKey}";
        var dto = await _http.GetFromJsonAsync<OmdbDetailDto>(url);

        if (dto == null || dto.Response == "False")
            return null;

        return new Film
        {
            Title = dto.Title ?? "",
            Year = int.TryParse(dto.Year, out var y) ? y : null,
            Plot = dto.Plot,
            PosterUrl = dto.Poster
        };
    }

    // ===== DTOs =====

    public class OmdbSearchItem
    {
        public string Title { get; set; } = "";
        public string Year { get; set; } = "";
        public string ImdbId { get; set; } = "";
        public string? Poster { get; set; }
        public override string ToString() => string.IsNullOrWhiteSpace(Year) ? Title : $"{Title} ({Year})";
    }

    private class SearchResponse
    {
        [JsonPropertyName("Search")]
        public List<SearchItemDto>? Search { get; set; }

        public string? Response { get; set; }

        [JsonPropertyName("Error")]
        public string? Error { get; set; }
    }

    private class SearchItemDto
    {
        public string? Title { get; set; }
        public string? Year { get; set; }
        public string? imdbID { get; set; }
        public string? Poster { get; set; }
    }

    private class OmdbDetailDto
    {
        public string? Title { get; set; }
        public string? Year { get; set; }
        public string? Plot { get; set; }
        public string? Poster { get; set; }
        public string? Response { get; set; }

        [JsonPropertyName("Error")]
        public string? Error { get; set; }
    }

    // (valfritt) behåll din gamla GetByTitleAsync om du använder den
}
