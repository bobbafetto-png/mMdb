// MainViewModel – central app-logik (filmsamling, OMDb, lagring) – ROBUST (Work/Edition/Copy)

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using mMdb.Models;
using mMdb.Services;

namespace mMdb.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // UI-lista (flattenad vy av biblioteket)
        public ObservableCollection<LibraryListItem> Films { get; } = new();

        // OMDb-klient
        private readonly OmdbClient _omdb =
            new(new HttpClient(), "ad1fdad7");

        // Bibliotek (robust struktur)
        private LibraryFile _library = new();

        private LibraryListItem? _selectedFilm;
        public LibraryListItem? SelectedFilm
        {
            get => _selectedFilm;
            set
            {
                if (_selectedFilm != value)
                {
                    _selectedFilm = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _query = "";
        public string Query
        {
            get => _query;
            set
            {
                if (_query != value)
                {
                    _query = value;
                    OnPropertyChanged();
                    ApplyFilter();
                }
            }
        }

        // Intern lista för filtrering
        private readonly List<LibraryListItem> _allItems = new();

        // Debounced save
        private CancellationTokenSource? _saveCts;

        public MainViewModel()
        {
            _ = InitializeAsync();
        }

        // Sök i OMDb (används av din UI för att visa sökresultat)
        public async Task<List<OmdbClient.OmdbSearchItem>> SearchOmdbAsync(string query)
            => await _omdb.SearchAsync(query);

        // Lägg till från OMDb med IMDb-id
        public async Task AddFromOmdbImdbIdAsync(string imdbId)
        {
            // Hämta film från OMDb
            var film = await _omdb.GetByImdbIdAsync(imdbId);
            if (film == null)
                return;

            // Sätt robust ExternalId (används för dedupe på Work-nivå)
            film.ExternalId = $"imdb:{imdbId}";

            // ROBUST ADD:
            // - Work dedupe via ExternalId
            // - Edition skapas (utan barcode/label/region/cut tills du lägger UI för det)
            // - Copy skapas/ökar Quantity på formatnivå
            var work = LibraryService.AddOrUpdate(
                _library,
                film,
                format: MediaFormat.Other
            );

            // Bygg om listan och välj “senast påverkad”
            RebuildFlatList();
            ApplyFilter();

            // Sätt SelectedFilm till första item som hör till work
            SelectedFilm = Films.FirstOrDefault(x => x.WorkId == work.Id);

            ScheduleSave();
        }

        // Ta bort vald (minskar Quantity eller tar bort copy/edition/work vid behov)
        public void RemoveSelectedFilm()
        {
            if (SelectedFilm == null)
                return;

            var item = SelectedFilm;

            var work = _library.Works.FirstOrDefault(w => w.Id == item.WorkId);
            if (work == null)
                return;

            var edition = work.Editions.FirstOrDefault(e => e.Id == item.EditionId);
            if (edition == null)
                return;

            // Hitta copy med formatet som matchar list-raden
            var copy = edition.Copies.FirstOrDefault(c => c.Format == item.Format);
            if (copy == null)
                return;

            // Minska quantity, eller ta bort copy helt
            if (copy.Quantity > 1)
            {
                copy.Quantity -= 1;
            }
            else
            {
                edition.Copies.Remove(copy);
            }

            // Städa tomma nivåer
            if (edition.Copies.Count == 0)
                work.Editions.Remove(edition);

            if (work.Editions.Count == 0)
                _library.Works.Remove(work);

            // Uppdatera UI
            RebuildFlatList();
            ApplyFilter();

            SelectedFilm = Films.FirstOrDefault();

            ScheduleSave();
        }

        private async Task InitializeAsync()
        {
            _library = await LibraryStore.LoadAsync();
            RebuildFlatList();
            ApplyFilter();
            SelectedFilm = Films.FirstOrDefault();
        }

        // Bygger en platt lista för UI:
        // 1 rad per (Work + Edition + Copy-format), med Quantity summerat per Copy
        private void RebuildFlatList()
        {
            _allItems.Clear();

            foreach (var w in _library.Works)
            {
                foreach (var e in w.Editions)
                {
                    foreach (var c in e.Copies)
                    {
                        _allItems.Add(new LibraryListItem
                        {
                            WorkId = w.Id,
                            Title = w.Title,
                            Year = w.Year,
                            PosterUrl = w.PosterUrl,
                            ExternalId = w.ExternalId,

                            EditionId = e.Id,
                            Format = c.Format,
                            Quantity = c.Quantity
                        });
                    }
                }
            }

            // Valfritt: sortera
            _allItems.Sort((a, b) =>
            {
                var t = string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
                if (t != 0) return t;

                var ya = a.Year ?? 0;
                var yb = b.Year ?? 0;
                return yb.CompareTo(ya);
            });
        }

        private void ApplyFilter()
        {
            var q = Query?.Trim() ?? "";

            var filtered = string.IsNullOrWhiteSpace(q)
                ? _allItems
                : _allItems.Where(f =>
                    f.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (f.Year?.ToString().Contains(q) ?? false) ||
                    f.Format.ToString().Contains(q, StringComparison.OrdinalIgnoreCase));

            Films.Clear();
            foreach (var film in filtered)
                Films.Add(film);

            if (SelectedFilm == null || !Films.Contains(SelectedFilm))
                SelectedFilm = Films.FirstOrDefault();
        }

        private void ScheduleSave()
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, token);
                    await LibraryStore.SaveAsync(_library);
                }
                catch (TaskCanceledException)
                {
                }
            }, token);
        }
    }
}
