using Google.Protobuf;
using Opure.Workspace.Contracts;
using Opure.Workspace.Protocol.Snapshot.V1;
using Xunit;

namespace Opure.Workspace.Protocol.Tests;

public sealed class WorkspaceSnapshotContractTests
{
    [Fact]
    public void DomainAndWireBoundsStayAligned()
    {
        Assert.Equal(
            WorkspaceSnapshotBounds.MaximumFileCount,
            checked((int)WorkspaceSnapshotContractPolicy.MaximumFileCount));
        Assert.Equal(
            WorkspaceSnapshotBounds.MaximumObservedBytes,
            checked((long)WorkspaceSnapshotContractPolicy.MaximumObservedBytes));
        Assert.Equal(
            WorkspaceSnapshotBounds.MaximumDuration,
            TimeSpan.FromMilliseconds(
                WorkspaceSnapshotContractPolicy.MaximumDurationMilliseconds));
    }

    [Fact]
    public void SchemaFixtureRoundTripsWithinBounds()
    {
        CreateWorkspaceSnapshotRequest request = CreateRequest();
        WorkspaceSnapshotResponse response = CreateCompleteResponse();

        byte[] requestWire = request.ToByteArray();
        byte[] responseWire = response.ToByteArray();
        CreateWorkspaceSnapshotRequest parsedRequest =
            CreateWorkspaceSnapshotRequest.Parser.ParseFrom(requestWire);
        WorkspaceSnapshotResponse parsedResponse =
            WorkspaceSnapshotResponse.Parser.ParseFrom(responseWire);

        Assert.Equal(request, parsedRequest);
        Assert.Equal(response, parsedResponse);
        Assert.InRange(
            requestWire.Length,
            1,
            WorkspaceSnapshotContractPolicy.MaximumRequestBytes);
        Assert.InRange(
            responseWire.Length,
            1,
            WorkspaceSnapshotContractPolicy.MaximumResponseBytes);
        Assert.True(
            WorkspaceSnapshotContractPolicy.Validate(parsedRequest).IsValid);
        Assert.True(
            WorkspaceSnapshotContractPolicy.Validate(
                parsedRequest,
                parsedResponse).IsValid);
    }

    [Fact]
    public void CrossProjectResponseIsDenied()
    {
        CreateWorkspaceSnapshotRequest request = CreateRequest();
        WorkspaceSnapshotResponse response = CreateCompleteResponse();
        response.Snapshot.ProjectId =
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        WorkspaceSnapshotValidationResult result =
            WorkspaceSnapshotContractPolicy.Validate(request, response);

        Assert.False(result.IsValid);
        Assert.Equal(
            WorkspaceSnapshotErrorCodes.CrossProjectDenied,
            result.ErrorCode);
    }

    [Fact]
    public void ExcessiveRequestedLimitsAreRejected()
    {
        CreateWorkspaceSnapshotRequest request = CreateRequest();
        request.Limits.MaximumFileCount =
            WorkspaceSnapshotContractPolicy.MaximumFileCount + 1;

        WorkspaceSnapshotValidationResult result =
            WorkspaceSnapshotContractPolicy.Validate(request);

        Assert.False(result.IsValid);
        Assert.Equal(
            WorkspaceSnapshotErrorCodes.InvalidLimits,
            result.ErrorCode);
    }

    [Fact]
    public void CancellationFixtureCannotClaimCompletionOrCurrency()
    {
        CreateWorkspaceSnapshotRequest request = CreateRequest();
        WorkspaceSnapshotResponse response = new()
        {
            ContractRevision = WorkspaceSnapshotContractPolicy.CurrentRevision,
            Snapshot = new WorkspaceSnapshot
            {
                ProjectId = request.ProjectId,
                RootReferenceId = request.RootReferenceId,
                State = WorkspaceSnapshotState.Cancelled,
                Current = false
            }
        };

        WorkspaceSnapshotValidationResult cancelled =
            WorkspaceSnapshotContractPolicy.Validate(request, response);
        response.Snapshot.State = WorkspaceSnapshotState.Complete;
        WorkspaceSnapshotValidationResult misleading =
            WorkspaceSnapshotContractPolicy.Validate(request, response);

        Assert.True(cancelled.IsValid);
        Assert.False(misleading.IsValid);
        Assert.Equal(
            WorkspaceSnapshotErrorCodes.MisleadingCompletion,
            misleading.ErrorCode);
    }

    [Fact]
    public void UnsupportedFileClassHasSafeStableRepresentation()
    {
        CreateWorkspaceSnapshotRequest request = CreateRequest();
        WorkspaceSnapshotResponse response = CreateCompleteResponse();
        response.Snapshot.Files.Add(new WorkspaceFileEntry
        {
            LogicalPath = "device-entry",
            FileClass = WorkspaceFileClass.Unsupported,
            FileIdentitySha256 = new string('a', 64),
            ObservedSizeBytes = 0,
            UnsupportedTypeCode = "WINDOWS_REPARSE_UNSUPPORTED"
        });
        response.Snapshot.ObservedFileCount = 1;

        WorkspaceSnapshotValidationResult result =
            WorkspaceSnapshotContractPolicy.Validate(request, response);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(':', response.Snapshot.Files[0].LogicalPath);
        Assert.Empty(response.Snapshot.Files[0].ContentSha256);
    }

    [Fact]
    public void UnknownFileClassReturnsStableErrorWithoutThrowing()
    {
        CreateWorkspaceSnapshotRequest request = CreateRequest();
        WorkspaceSnapshotResponse response = CreateCompleteResponse();
        response.Snapshot.Files.Add(new WorkspaceFileEntry
        {
            LogicalPath = "unknown-entry",
            FileClass = (WorkspaceFileClass)99,
            FileIdentitySha256 = new string('b', 64)
        });
        response.Snapshot.ObservedFileCount = 1;

        WorkspaceSnapshotValidationResult result =
            WorkspaceSnapshotContractPolicy.Validate(request, response);

        Assert.False(result.IsValid);
        Assert.Equal(
            WorkspaceSnapshotErrorCodes.UnknownFileClass,
            result.ErrorCode);
    }

    private static CreateWorkspaceSnapshotRequest CreateRequest()
    {
        return new CreateWorkspaceSnapshotRequest
        {
            MinimumContractRevision =
                WorkspaceSnapshotContractPolicy.CurrentRevision,
            MaximumContractRevision =
                WorkspaceSnapshotContractPolicy.CurrentRevision,
            OperationId = "0123456789abcdef0123456789abcdef",
            CorrelationId = "abcdef0123456789abcdef0123456789",
            ProjectId = "11111111111111111111111111111111",
            RootReferenceId = "22222222222222222222222222222222",
            Limits = new WorkspaceSnapshotLimits
            {
                MaximumFileCount =
                    WorkspaceSnapshotContractPolicy.MaximumFileCount,
                MaximumObservedBytes =
                    WorkspaceSnapshotContractPolicy.MaximumObservedBytes,
                MaximumDurationMilliseconds =
                    WorkspaceSnapshotContractPolicy.MaximumDurationMilliseconds
            }
        };
    }

    private static WorkspaceSnapshotResponse CreateCompleteResponse()
    {
        return new WorkspaceSnapshotResponse
        {
            ContractRevision = WorkspaceSnapshotContractPolicy.CurrentRevision,
            Snapshot = new WorkspaceSnapshot
            {
                ProjectId = "11111111111111111111111111111111",
                RootReferenceId = "22222222222222222222222222222222",
                Generation = 1,
                State = WorkspaceSnapshotState.Complete,
                Current = true,
                ObservedUnixTimeMilliseconds = 1_786_000_000_000,
                Repository = new WorkspaceRepositorySummary
                {
                    RepositoryClass = WorkspaceRepositoryClass.None,
                    SafeStateCode = "NONE"
                }
            }
        };
    }
}
