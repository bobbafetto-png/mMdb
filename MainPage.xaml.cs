using System.Collections.Specialized;
using mMdb.ViewModels;

namespace mMdb;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        HookViewModelEvents();
        UpdateButtons();
    }

    private void HookViewModelEvents()
    {
        if (BindingContext is not MainViewModel vm)
            return;

        // Uppdatera när SelectedFilm/Query etc ändras
        vm.PropertyChanged += (_, __) => MainThread.BeginInvokeOnMainThread(UpdateButtons);

        // Uppdatera när listan blir tom/icke-tom
        vm.Films.CollectionChanged += Films_CollectionChanged;
    }

    private void Films_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => MainThread.BeginInvokeOnMainThread(UpdateButtons);

    private async void OnFetchClicked(object sender, EventArgs e)
    {
        if (BindingContext is not MainViewModel vm)
            return;

        var q = vm.Query?.Trim();
        if (string.IsNullOrWhiteSpace(q))
            return;

        var results = await vm.SearchOmdbAsync(q);

        if (results.Count == 0)
        {
            await DisplayAlert("Inga träffar", "OMDb hittade inga filmer på din sökning.", "OK");
            UpdateButtons();
            return;
        }

        // Visa val-lista (titel + år)
        var options = results.Select(r => r.ToString()).ToArray();
        var choice = await DisplayActionSheet("Välj film att lägga till", "Avbryt", null, options);

        if (string.IsNullOrWhiteSpace(choice) || choice == "Avbryt")
            return;

        var selected = results.FirstOrDefault(r => r.ToString() == choice);
        if (selected == null)
            return;

        await vm.AddFromOmdbImdbIdAsync(selected.ImdbId);
        UpdateButtons();
    }


    private async void OnRemoveClicked(object sender, EventArgs e)
    {
        if (BindingContext is not MainViewModel vm)
            return;

        if (vm.SelectedFilm == null)
            return;

        var title = vm.SelectedFilm.Title;

        var ok = await DisplayAlert(
            "Ta bort film",
            $"Vill du ta bort \"{title}\"?",
            "Ta bort",
            "Avbryt");

        if (!ok)
            return;

        vm.RemoveSelectedFilm();
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (BindingContext is not MainViewModel vm)
            return;

        // Synlig bara om det finns något i listan
        RemoveBtn.IsVisible = vm.Films.Count > 0;

        // Aktiverad bara om en film är vald
        RemoveBtn.IsEnabled = vm.SelectedFilm != null;
    }
}
