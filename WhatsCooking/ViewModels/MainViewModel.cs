using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using BBRepoList.Abstractions;
using BBRepoList.Configuration;
using BBRepoList.Models;

using Microsoft.Extensions.Options;

using WhatsCooking.Services;

namespace WhatsCooking.ViewModels;

/// <summary>
/// View model for the pull request dashboard window.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "View model is created by dependency injection.")]
internal sealed class MainViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="loader">Pull request dashboard data loader.</param>
    /// <param name="telemetryService">Bitbucket API telemetry service.</param>
    /// <param name="options">Bitbucket configuration options.</param>
    public MainViewModel(PullRequestDashboardLoader loader, IBitbucketTelemetryService telemetryService, IOptions<BitbucketOptions> options)
    {
        ArgumentNullException.ThrowIfNull(loader, nameof(loader));
        ArgumentNullException.ThrowIfNull(telemetryService, nameof(telemetryService));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        _loader = loader;
        _telemetryService = telemetryService;
        _options = options.Value;
        _selectedSearchMode = RepositorySearchMode.StartWith;
        _searchPhrase = string.Empty;
        _mergedPullRequestsDays = 1;
        OpenPullRequestsView = CollectionViewSource.GetDefaultView(OpenPullRequests);
        MergedPullRequestsView = CollectionViewSource.GetDefaultView(MergedPullRequests);
        TelemetryView = CollectionViewSource.GetDefaultView(Telemetry);
        OpenPullRequestsView.Filter = FilterPullRequestRow;
        MergedPullRequestsView.Filter = FilterPullRequestRow;
        TelemetryView.Filter = FilterTelemetryRow;
        LoadCommand = new RelayCommand(async () => await LoadAsync().ConfigureAwait(false), CanLoad);
        CancelCommand = new RelayCommand(Cancel, () => IsLoading);
        OpenUrlCommand = new RelayCommand(OpenUrl);
        ResetFiltersCommand = new RelayCommand(ResetFilters);
        RefreshTelemetry();
    }

    /// <summary>
    /// Repository search modes available in the UI.
    /// </summary>
    public static Array SearchModes => Enum.GetValues<RepositorySearchMode>();

    /// <summary>
    /// Selected repository search mode.
    /// </summary>
    public RepositorySearchMode SelectedSearchMode {
        get => _selectedSearchMode;
        set => SetProperty(ref _selectedSearchMode, value);
    }

    /// <summary>
    /// Repository search phrase entered by the user.
    /// </summary>
    public string SearchPhrase {
        get => _searchPhrase;
        set => SetProperty(ref _searchPhrase, value);
    }

    /// <summary>
    /// Number of days used to load recently merged pull requests.
    /// </summary>
    public int MergedPullRequestsDays {
        get => _mergedPullRequestsDays;
        set
        {
            if (SetProperty(ref _mergedPullRequestsDays, Math.Max(value, 1)))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Current application status text.
    /// </summary>
    public string Status {
        get;
        private set => SetProperty(ref field, value);
    } = "Ready";

    /// <summary>
    /// Global text filter applied to pull request tables.
    /// </summary>
    public string GlobalSearch {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OpenPullRequestsView.Refresh();
                MergedPullRequestsView.Refresh();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Filter for the row number column.
    /// </summary>
    public string NumberFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the repository column.
    /// </summary>
    public string RepositoryFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the pull request column.
    /// </summary>
    public string PullRequestFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the author column.
    /// </summary>
    public string AuthorFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the description length column.
    /// </summary>
    public string DescriptionLengthFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the open duration column.
    /// </summary>
    public string OpenForFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the time to first response column.
    /// </summary>
    public string TimeToFirstResponseFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the latest activity or merge age column.
    /// </summary>
    public string ActivityFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the comments column.
    /// </summary>
    public string CommentsFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the request changes column.
    /// </summary>
    public string RequestChangesFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the approvals column.
    /// </summary>
    public string ApprovalsFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Filter for the current user activity column.
    /// </summary>
    public string CurrentUserActivityFilter {
        get;
        set => SetFilterProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Text filter applied to the telemetry table.
    /// </summary>
    public string TelemetryFilter {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                TelemetryView.Refresh();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Whether pull request data is currently loading.
    /// </summary>
    public bool IsLoading {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// Number of repositories matched by the current repository filter.
    /// </summary>
    public int RepositoriesCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of open pull requests loaded into the dashboard.
    /// </summary>
    public int OpenPullRequestsCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of recently merged pull requests loaded into the dashboard.
    /// </summary>
    public int MergedPullRequestsCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of Bitbucket API requests captured by telemetry.
    /// </summary>
    public int TelemetryRequestsCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Number of Bitbucket API endpoints captured by telemetry.
    /// </summary>
    public int TelemetryEndpointsCount {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Whether Bitbucket API telemetry is enabled.
    /// </summary>
    public bool IsTelemetryEnabled {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Loaded open pull request rows.
    /// </summary>
    public ObservableCollection<PullRequestRow> OpenPullRequests { get; } = [];

    /// <summary>
    /// Loaded recently merged pull request rows.
    /// </summary>
    public ObservableCollection<PullRequestRow> MergedPullRequests { get; } = [];

    /// <summary>
    /// Loaded telemetry rows.
    /// </summary>
    public ObservableCollection<TelemetryRow> Telemetry { get; } = [];

    /// <summary>
    /// Filterable view over open pull request rows.
    /// </summary>
    public ICollectionView OpenPullRequestsView { get; }

    /// <summary>
    /// Filterable view over recently merged pull request rows.
    /// </summary>
    public ICollectionView MergedPullRequestsView { get; }

    /// <summary>
    /// Filterable view over telemetry rows.
    /// </summary>
    public ICollectionView TelemetryView { get; }

    /// <summary>
    /// Command that loads pull request data from Bitbucket.
    /// </summary>
    public ICommand LoadCommand { get; }

    /// <summary>
    /// Command that cancels the current pull request loading operation.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Command that opens a Bitbucket URL in the default browser.
    /// </summary>
    public ICommand OpenUrlCommand { get; }

    /// <summary>
    /// Command that clears all table filters.
    /// </summary>
    public ICommand ResetFiltersCommand { get; }

    private async Task LoadAsync()
    {
        if (!CanLoad())
        {
            return;
        }

        if (HasLoadedPullRequests() && !ConfirmReload())
        {
            return;
        }

        _loadCancellation = new CancellationTokenSource();
        IsLoading = true;
        Status = "Starting";
        try
        {
            var filterPattern = new FilterPattern(SearchPhrase, SelectedSearchMode);
            var progress = new Progress<PullRequestLoadProgress>(value =>
            {
                Status = value.Message;
                RefreshTelemetry();
            });
            var result = await _loader.LoadAsync(filterPattern, MergedPullRequestsDays, progress, _loadCancellation.Token).ConfigureAwait(true);
            var asOf = DateTimeOffset.Now;
            OpenPullRequests.Clear();
            MergedPullRequests.Clear();
            for (var i = 0; i < result.OpenPullRequests.Count; i++)
            {
                OpenPullRequests.Add(new PullRequestRow(i + 1, result.OpenPullRequests[i], asOf, _options));
            }
            for (var i = 0; i < result.MergedPullRequests.Count; i++)
            {
                MergedPullRequests.Add(new PullRequestRow(i + 1, result.MergedPullRequests[i], _options));
            }
            RepositoriesCount = result.Repositories.Count;
            OpenPullRequestsCount = result.OpenPullRequests.Count;
            MergedPullRequestsCount = result.MergedPullRequests.Count;
            RefreshTelemetry();
            Status = $"Loaded {OpenPullRequestsCount} open PRs and {MergedPullRequestsCount} merged PRs";
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
        }
        catch (HttpRequestException ex)
        {
            ShowLoadError(ex);
        }
        catch (JsonException ex)
        {
            ShowLoadError(ex);
        }
        catch (InvalidOperationException ex)
        {
            ShowLoadError(ex);
        }
        catch (ArgumentException ex)
        {
            ShowLoadError(ex);
        }
        finally
        {
            _loadCancellation.Dispose();
            _loadCancellation = null;
            IsLoading = false;
        }
    }

    private bool CanLoad() => !IsLoading && MergedPullRequestsDays > 0;

    private bool HasLoadedPullRequests() => OpenPullRequests.Count > 0 || MergedPullRequests.Count > 0;

    private static bool ConfirmReload()
    {
        var result = MessageBox.Show(
            "Pull requests are already loaded. Reload data from Bitbucket?",
            "Reload data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    private void Cancel() => _loadCancellation?.Cancel();

    private void OpenUrl(object? parameter)
    {
        if (parameter is not Uri url)
        {
            return;
        }
        _ = Process.Start(new ProcessStartInfo(url.ToString())
        {
            UseShellExecute = true
        });
    }

    private bool FilterPullRequestRow(object item)
    {
        if (item is not PullRequestRow row)
        {
            return false;
        }
        return Matches(row.SearchText, GlobalSearch)
            && Matches(row.Number.ToString(CultureInfo.InvariantCulture), NumberFilter)
            && Matches(row.RepositoryName, RepositoryFilter)
            && Matches(row.PullRequestDisplay, PullRequestFilter)
            && Matches(row.Author, AuthorFilter)
            && Matches(row.DescriptionLength.ToString(CultureInfo.InvariantCulture), DescriptionLengthFilter)
            && Matches(row.OpenFor, OpenForFilter)
            && Matches(row.TimeToFirstResponse, TimeToFirstResponseFilter)
            && Matches(row.ActivityAgeOrMerged, ActivityFilter)
            && Matches(row.CommentsCount.ToString(CultureInfo.InvariantCulture), CommentsFilter)
            && Matches(row.RequestChanges, RequestChangesFilter)
            && Matches(row.Approvals, ApprovalsFilter)
            && Matches(row.CurrentUserActivity, CurrentUserActivityFilter);
    }

    private bool FilterTelemetryRow(object item) => item is TelemetryRow row && Matches(row.SearchText, TelemetryFilter);

    private void ShowLoadError(Exception exception)
    {
        Status = exception.Message;
        _ = MessageBox.Show(exception.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void RefreshTelemetry()
    {
        var snapshot = _telemetryService.GetSnapshot();
        IsTelemetryEnabled = snapshot.IsEnabled;
        TelemetryRequestsCount = snapshot.TotalRequests;
        TelemetryEndpointsCount = snapshot.RequestStatistics.Count;
        Telemetry.Clear();
        for (var i = 0; i < snapshot.RequestStatistics.Count; i++)
        {
            Telemetry.Add(new TelemetryRow(i + 1, snapshot.RequestStatistics[i], snapshot.TotalRequests));
        }
        TelemetryView.Refresh();
    }

    private bool SetFilterProperty(ref string field, string value)
    {
        if (!SetProperty(ref field, value))
        {
            return false;
        }
        RefreshViews();
        return true;
    }

    private void RefreshViews()
    {
        OpenPullRequestsView.Refresh();
        MergedPullRequestsView.Refresh();
    }

    private void ResetFilters()
    {
        GlobalSearch = string.Empty;
        NumberFilter = string.Empty;
        RepositoryFilter = string.Empty;
        PullRequestFilter = string.Empty;
        AuthorFilter = string.Empty;
        DescriptionLengthFilter = string.Empty;
        OpenForFilter = string.Empty;
        TimeToFirstResponseFilter = string.Empty;
        ActivityFilter = string.Empty;
        CommentsFilter = string.Empty;
        RequestChangesFilter = string.Empty;
        ApprovalsFilter = string.Empty;
        CurrentUserActivityFilter = string.Empty;
        TelemetryFilter = string.Empty;
    }

    private static bool Matches(string source, string filter) => string.IsNullOrWhiteSpace(filter) || source.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private void RaiseCommandStates()
    {
        ((RelayCommand)LoadCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Releases resources held by the view model.
    /// </summary>
    public void Dispose() => _loadCancellation?.Dispose();

    private readonly PullRequestDashboardLoader _loader;

    private readonly BitbucketOptions _options;

    private readonly IBitbucketTelemetryService _telemetryService;

    private int _mergedPullRequestsDays;

    private CancellationTokenSource? _loadCancellation;

    private string _searchPhrase;

    private RepositorySearchMode _selectedSearchMode;
}
