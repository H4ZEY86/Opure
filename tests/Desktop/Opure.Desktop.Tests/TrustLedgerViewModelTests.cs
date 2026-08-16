using System;
using System.Collections.Generic;
using System.Threading;
using Opure.Desktop.Contracts;
using Xunit;

namespace Opure.Desktop.Tests;

public sealed class TrustLedgerViewModelTests
{
    private sealed class MockTrustLedgerSource : ITrustLedgerSource
    {
        private readonly List<TrustReceiptItem> _receipts = new();

        public event EventHandler<TrustReceiptItem>? ReceiptAdded;

        public IReadOnlyList<TrustReceiptItem> GetHistoricalReceipts() => _receipts.ToArray();

        public void PushReceipt(TrustReceiptItem item)
        {
            _receipts.Add(item);
            ReceiptAdded?.Invoke(this, item);
        }
    }

    [Fact]
    public void Constructor_LoadsHistoricalReceipts()
    {
        var source = new MockTrustLedgerSource();
        source.PushReceipt(new TrustReceiptItem("1", "T1", "A1", "F1", "S1", "V1"));
        source.PushReceipt(new TrustReceiptItem("2", "T2", "A2", "F2", "S2", "V2"));

        using var viewModel = new TrustLedgerViewModel(source);
        
        Assert.Equal(2, viewModel.Receipts.Count);
        // Latest at the top
        Assert.Equal("2", viewModel.Receipts[0].ReceiptId);
        Assert.Equal("1", viewModel.Receipts[1].ReceiptId);
    }

    [Fact]
    public void ReceiptAddedEvent_AddsToCollection()
    {
        var source = new MockTrustLedgerSource();
        using var viewModel = new TrustLedgerViewModel(source);

        Assert.Empty(viewModel.Receipts);

        source.PushReceipt(new TrustReceiptItem("3", "T3", "A3", "F3", "S3", "V3"));

        Assert.Single(viewModel.Receipts);
        Assert.Equal("3", viewModel.Receipts[0].ReceiptId);
    }

    [Fact]
    public void FilterText_FiltersReceipts()
    {
        var source = new MockTrustLedgerSource();
        source.PushReceipt(new TrustReceiptItem("id1", "T1", "User:Dev", "file1.txt", "S1", "Verified"));
        source.PushReceipt(new TrustReceiptItem("id2", "T2", "Agent:Test", "file2.txt", "S2", "Rollback Available"));

        using var viewModel = new TrustLedgerViewModel(source);
        Assert.Equal(2, viewModel.Receipts.Count);

        viewModel.FilterText = "Dev";
        Assert.Single(viewModel.Receipts);
        Assert.Equal("id1", viewModel.Receipts[0].ReceiptId);

        viewModel.FilterText = "file2";
        Assert.Single(viewModel.Receipts);
        Assert.Equal("id2", viewModel.Receipts[0].ReceiptId);

        viewModel.FilterText = "Verified";
        Assert.Single(viewModel.Receipts);
        Assert.Equal("id1", viewModel.Receipts[0].ReceiptId);

        viewModel.FilterText = "";
        Assert.Equal(2, viewModel.Receipts.Count);
    }
}
