using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class PatchReviewViewModelTests
{
    [Fact]
    public async Task ApproveCommand_SetsResultToApprovedWithIdentity()
    {
        var viewModel = new PatchReviewViewModel("test.txt", new List<DiffLineItem>(), 1, 1);
        
        viewModel.ApproveCommand.Execute(null);
        
        var result = await viewModel.ResultTask;
        Assert.True(result.IsApproved);
        Assert.Equal("User:Developer", result.ApproverIdentity);
        Assert.Null(result.Feedback);
    }

    [Fact]
    public async Task RejectCommand_SetsResultToRejectedWithFeedback()
    {
        var viewModel = new PatchReviewViewModel("test.txt", new List<DiffLineItem>(), 1, 1)
        {
            Feedback = "Needs changes"
        };
        
        viewModel.RejectCommand.Execute(null);
        
        var result = await viewModel.ResultTask;
        Assert.False(result.IsApproved);
        Assert.Null(result.ApproverIdentity);
        Assert.Equal("Needs changes", result.Feedback);
    }
}
