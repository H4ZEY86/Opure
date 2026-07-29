using Google.Protobuf;
using Opure.Project.Protocol;
using Opure.Project.Protocol.Open.V1;
using Xunit;

namespace Opure.Project.Sqlite.Tests;

public sealed class ProjectOpenContractTests
{
    [Fact]
    public void RequestFixtureRoundTripsWithinBound()
    {
        OpenProjectRequest expected = CreateRequest();

        byte[] wire = expected.ToByteArray();
        OpenProjectRequest parsed =
            OpenProjectRequest.Parser.ParseFrom(wire);

        Assert.Equal(expected, parsed);
        Assert.InRange(
            wire.Length,
            1,
            ProjectOpenContractPolicy.MaximumRequestBytes);
        Assert.True(
            ProjectOpenContractPolicy.ValidateRequest(parsed).IsValid);
    }

    [Fact]
    public void ResponseFixtureRoundTripsWithinBound()
    {
        OpenProjectResponse expected = CreateResponse();

        byte[] wire = expected.ToByteArray();
        OpenProjectResponse parsed =
            OpenProjectResponse.Parser.ParseFrom(wire);

        Assert.Equal(expected, parsed);
        Assert.InRange(
            wire.Length,
            1,
            ProjectOpenContractPolicy.MaximumResponseBytes);
        Assert.True(
            ProjectOpenContractPolicy.ValidateResponse(parsed).IsValid);
    }

    [Fact]
    public void UnknownEnumValueIsRejected()
    {
        OpenProjectRequest request = CreateRequest();
        request.ReleaseChannel = (ProjectReleaseChannel)99;

        ProjectOpenValidationResult result =
            ProjectOpenContractPolicy.ValidateRequest(request);

        Assert.False(result.IsValid);
        Assert.Equal(
            ProjectOpenErrorCodes.InvalidReleaseChannel,
            result.ErrorCode);
    }

    [Fact]
    public void IncompatibleRevisionReturnsStableError()
    {
        OpenProjectRequest request = CreateRequest();
        request.MinimumContractRevision =
            ProjectOpenContractPolicy.CurrentRevision + 1;
        request.MaximumContractRevision =
            ProjectOpenContractPolicy.CurrentRevision + 1;

        ProjectOpenValidationResult validation =
            ProjectOpenContractPolicy.ValidateRequest(request);
        OpenProjectResponse response =
            ProjectOpenContractPolicy
                .CreateIncompatibleRevisionResponse();

        Assert.False(validation.IsValid);
        Assert.Equal(
            ProjectOpenErrorCodes.IncompatibleContract,
            validation.ErrorCode);
        Assert.Equal(
            OpenProjectErrorCategory.IncompatibleContract,
            response.Error.Category);
        Assert.True(
            ProjectOpenContractPolicy.ValidateResponse(response).IsValid);
    }

    private static OpenProjectRequest CreateRequest()
    {
        return new OpenProjectRequest
        {
            MinimumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            MaximumContractRevision =
                ProjectOpenContractPolicy.CurrentRevision,
            OperationId = "0123456789abcdef0123456789abcdef",
            CorrelationId = "abcdef0123456789abcdef0123456789",
            ReleaseChannel = ProjectReleaseChannel.Development,
            DisplayName = "Fixture project",
            Root = new ProjectRootIdentityClaim
            {
                DisplayPath = @"C:\fixture\project",
                VolumeClass = FilesystemVolumeClass.FixedLocal,
                VolumeSerialNumber = 42,
                FileId = "00112233445566778899aabbccddeeff",
                IdentityCapability =
                    FileIdentityCapability.WindowsFileId128
            }
        };
    }

    private static OpenProjectResponse CreateResponse()
    {
        return new OpenProjectResponse
        {
            ContractRevision = ProjectOpenContractPolicy.CurrentRevision,
            Project = new OpenProjectSummary
            {
                OperationId =
                    "0123456789abcdef0123456789abcdef",
                ProjectId =
                    "fedcba9876543210fedcba9876543210",
                DisplayName = "Fixture project",
                ReleaseChannel = ProjectReleaseChannel.Development,
                Disposition = ProjectOpenDisposition.Created,
                LifecycleState = ProjectLifecycleState.Open,
                RootVolumeClass = FilesystemVolumeClass.FixedLocal,
                InitialSnapshotState =
                    InitialWorkspaceSnapshotState.Ready,
                SafeDetail =
                    "The initial Workspace Snapshot is ready."
            }
        };
    }
}
