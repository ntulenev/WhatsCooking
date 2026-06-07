using WhatsCooking.Services;
using WhatsCooking.ViewModels;

using Xunit;

namespace WhatsCooking.Tests;

public sealed class PullRequestLoadProgressFormatterTests
{
    [Fact]
    public void FormatIncludesCountsWhenAvailable()
    {
        var progress = new PullRequestLoadProgress(
            PullRequestLoadStage.LoadingRepositories,
            3,
            10);

        Assert.Equal("Loading repositories: 3/10", PullRequestLoadProgressFormatter.Format(progress));
    }
}
