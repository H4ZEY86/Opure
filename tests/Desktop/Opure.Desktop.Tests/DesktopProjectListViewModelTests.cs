using System.Diagnostics;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class DesktopProjectListViewModelTests
{
    [Fact]
    public async Task EmptyProjectionShowsLocalFirstGuidance()
    {
        DesktopProjectListViewModel viewModel = new(new SequenceSource(CreateProjection([])));

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsEmpty);
        Assert.Equal("No registered projects", viewModel.StatusTitle);
        Assert.Contains("local folder", viewModel.StatusDetail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisconnectRetainsLastSuccessfulProjectionAsStale()
    {
        DesktopProjectListItem project = CreateProject("Retained", DesktopProjectAvailability.Available);
        SequenceSource source = new(CreateProjection([project]), failAfterFirstRefresh: true);
        DesktopProjectListViewModel viewModel = new(source);
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsStale);
        Assert.Single(viewModel.Projects);
        Assert.Equal("Projects — stale", viewModel.StatusTitle);
    }

    [Fact]
    public async Task LargeProjectionIsAppliedWithoutPerItemDesktopIo()
    {
        DesktopProjectListItem[] projects = Enumerable.Range(0, 10_000)
            .Select(index => CreateProject($"Project {index}", DesktopProjectAvailability.Available, index))
            .ToArray();
        DesktopProjectListViewModel viewModel = new(new SequenceSource(CreateProjection(projects)));
        Stopwatch stopwatch = Stopwatch.StartNew();

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        stopwatch.Stop();
        Assert.Equal(10_000, viewModel.Projects.Count);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task OpenAndRemoveCommandsUseSourceThenRefreshProjection()
    {
        DesktopProjectListItem project = CreateProject("Commanded", DesktopProjectAvailability.Available);
        SequenceSource source = new(CreateProjection([project]));
        DesktopProjectListViewModel viewModel = new(source);
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        viewModel.SelectedProject = project;

        await viewModel.OpenSelectedAsync(TestContext.Current.CancellationToken);
        viewModel.SelectedProject = project;
        await viewModel.RemoveSelectedRegistrationAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, source.OpenCount);
        Assert.Equal(1, source.RemoveCount);
    }

    private static DesktopProjectListProjection CreateProjection(IReadOnlyList<DesktopProjectListItem> projects) =>
        new(projects, DateTimeOffset.UtcNow);

    private static DesktopProjectListItem CreateProject(
        string name,
        DesktopProjectAvailability availability,
        int index = 1) => new(
            index.ToString("x32", System.Globalization.CultureInfo.InvariantCulture),
            name,
            "Fixed local storage",
            "Git repository",
            "Today",
            availability,
            availability == DesktopProjectAvailability.Available ? "Available" : "Unavailable",
            $"{name}, {availability}");

    private sealed class SequenceSource(
        DesktopProjectListProjection projection,
        bool failAfterFirstRefresh = false) : IDesktopProjectListSource
    {
        private int refreshCount;

        public int OpenCount { get; private set; }
        public int RemoveCount { get; private set; }

        public Task<DesktopProjectListProjection> RefreshAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            refreshCount++;
            return failAfterFirstRefresh && refreshCount > 1
                ? Task.FromException<DesktopProjectListProjection>(new InvalidOperationException("Disconnected"))
                : Task.FromResult(projection);
        }

        public Task<DesktopProjectCommandResult> OpenAsync(string projectId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenCount++;
            return Task.FromResult(new DesktopProjectCommandResult(true, "Opened."));
        }

        public Task<DesktopProjectCommandResult> RemoveRegistrationAsync(string projectId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RemoveCount++;
            return Task.FromResult(new DesktopProjectCommandResult(true, "Registration removed; files retained."));
        }
    }
}
