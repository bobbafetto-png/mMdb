using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using mMdb.Models;
using mMdb.ViewModels;

namespace mMdb;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        // Läs appsettings.json från app-paketet (MauiAsset)
        IConfiguration config;
        using (var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult())
        {
            config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
        }

        builder.Configuration.AddConfiguration(config);

        // === HÄR ÄR DEN VIKTIGA DELEN ===
        var apiKey = config["Omdb:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Omdb:ApiKey saknas i appsettings.json");

        var omdbOptions = new OmdbOptions
        {
            ApiKey = apiKey
        };

        builder.Services.AddSingleton<IOptions<OmdbOptions>>(
            Options.Create(omdbOptions)
        );

        // DI
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
