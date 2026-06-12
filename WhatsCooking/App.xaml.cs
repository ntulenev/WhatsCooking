using System.Windows;

using BBRepoList.Registrations;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WhatsCooking.Services;
using WhatsCooking.ViewModels;

namespace WhatsCooking;

/// <summary>
/// WPF application entry point.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected async override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureAppConfiguration(configuration =>
            {
                _ = configuration.SetBasePath(AppContext.BaseDirectory);
                _ = configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                _ = services.AddApplicationServices(context.Configuration);
                _ = services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
                _ = services.AddSingleton<IDialogService, WpfDialogService>();
                _ = services.AddSingleton<IExternalUrlLauncher, WpfExternalUrlLauncher>();
                _ = services.AddSingleton<IClipboardService, WpfClipboardService>();
                _ = services.AddSingleton<IAiReviewPromptService, AiReviewPromptService>();
                _ = services.AddSingleton<IDemoPullRequestDashboardProvider, DemoPullRequestDashboardProvider>();
                _ = services.AddSingleton<IDemoTelemetryProvider, DemoTelemetryProvider>();
                _ = services.AddTransient<IDebouncer, TimerDebouncer>();
                _ = services.AddTransient<IPullRequestDashboardLoader, PullRequestDashboardLoader>();
                _ = services.AddTransient<IDashboardLoadUseCase, DashboardLoadUseCase>();
                _ = services.AddSingleton<TelemetryViewModel>();
                _ = services.AddSingleton<PullRequestRowMapper>();
                _ = services.AddSingleton<MainViewModel>();
                _ = services.AddSingleton<MainWindow>();
            })
            .Build();
        await _host.StartAsync().ConfigureAwait(true);
        MainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
        base.OnStartup(e);
    }

    /// <inheritdoc />
    protected async override void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync().ConfigureAwait(true);
            _host.Dispose();
        }
        base.OnExit(e);
    }

    private IHost? _host;
}
