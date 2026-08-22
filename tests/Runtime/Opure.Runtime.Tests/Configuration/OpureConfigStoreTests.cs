using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Opure.Runtime.Configuration;
using Opure.Runtime.Contracts.Configuration;
using Xunit;

namespace Opure.Runtime.Tests.Configuration;

public sealed class OpureConfigStoreTests : IDisposable
{
    private readonly string _tempDir;

    public OpureConfigStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"opure-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    [Fact]
    public void GetBool_Returns_DefaultValue_When_Key_Absent()
    {
        var store = new OpureConfigStore(_tempDir);

        bool result = store.GetBool("NonExistentKey", defaultValue: true);

        Assert.True(result);
    }

    [Fact]
    public async Task SetBoolAsync_Persists_True_And_GetBool_Reads_It_Back()
    {
        var store = new OpureConfigStore(_tempDir);

        await store.SetBoolAsync(
            OpureConfigKeys.IsProActivated,
            true,
            TestContext.Current.CancellationToken);

        bool result = store.GetBool(OpureConfigKeys.IsProActivated);

        Assert.True(result);
    }

    [Fact]
    public async Task SetBoolAsync_Writes_File_In_DataRoot()
    {
        var store = new OpureConfigStore(_tempDir);

        await store.SetBoolAsync(
            "TestKey",
            true,
            TestContext.Current.CancellationToken);

        string configFile = Path.Combine(_tempDir, "config.json");
        Assert.True(File.Exists(configFile), "config.json must be written to the data root.");
    }

    [Fact]
    public async Task SetBoolAsync_Survives_Reload_From_Disk()
    {
        // Write with store A.
        var storeA = new OpureConfigStore(_tempDir);
        await storeA.SetBoolAsync(
            OpureConfigKeys.IsProActivated,
            true,
            TestContext.Current.CancellationToken);

        // Read with a fresh store B (forces a re-parse from disk).
        var storeB = new OpureConfigStore(_tempDir);
        bool result = storeB.GetBool(OpureConfigKeys.IsProActivated);

        Assert.True(result);
    }

    [Fact]
    public async Task GetBool_Returns_False_After_SetBool_False()
    {
        var store = new OpureConfigStore(_tempDir);

        await store.SetBoolAsync(
            OpureConfigKeys.IsProActivated,
            true,
            TestContext.Current.CancellationToken);

        await store.SetBoolAsync(
            OpureConfigKeys.IsProActivated,
            false,
            TestContext.Current.CancellationToken);

        bool result = store.GetBool(OpureConfigKeys.IsProActivated);

        Assert.False(result);
    }
}
