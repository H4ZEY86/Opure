using Google.Protobuf;
using Opure.Runtime.Contracts;
using Opure.Runtime.Contracts.Health.V1;
using Xunit;

namespace Opure.Runtime.Tests;

public sealed class RuntimeHealthContractTests
{
    private static readonly string QueryId = new('a', 32);
    private static readonly string CorrelationId = new('b', 32);
    private static readonly string BootId = new('c', 32);

    [Fact]
    public void Generated_contract_contains_client_and_server_surfaces()
    {
        Assert.NotNull(RuntimeHealthService.Descriptor);
        Assert.True(typeof(RuntimeHealthService.RuntimeHealthServiceBase).IsAbstract);
        Assert.False(typeof(RuntimeHealthService.RuntimeHealthServiceClient).IsAbstract);
    }

    [Fact]
    public void Request_and_response_round_trip_through_protobuf()
    {
        GetRuntimeHealthRequest request = CreateRequest();
        GetRuntimeHealthResponse response = CreateResponse();

        GetRuntimeHealthRequest parsedRequest =
            GetRuntimeHealthRequest.Parser.ParseFrom(request.ToByteArray());
        GetRuntimeHealthResponse parsedResponse =
            GetRuntimeHealthResponse.Parser.ParseFrom(response.ToByteArray());

        Assert.Equal(request, parsedRequest);
        Assert.Equal(response, parsedResponse);
        Assert.True(RuntimeHealthContractPolicy.ValidateRequest(parsedRequest).IsValid);
        Assert.True(RuntimeHealthContractPolicy.ValidateResponse(parsedResponse).IsValid);
    }

    [Fact]
    public void Unknown_fields_are_preserved_across_parse_and_serialize()
    {
        byte[] knownMessage = CreateResponse().ToByteArray();
        byte[] extendedMessage = [.. knownMessage, 0xF8, 0x07, 0x63];

        GetRuntimeHealthResponse parsed =
            GetRuntimeHealthResponse.Parser.ParseFrom(extendedMessage);

        Assert.Equal(extendedMessage, parsed.ToByteArray());
        Assert.True(RuntimeHealthContractPolicy.ValidateResponse(parsed).IsValid);
    }

    [Fact]
    public void Missing_boot_identity_fails_semantic_validation()
    {
        GetRuntimeHealthResponse response = CreateResponse();
        response.Health.RuntimeBootId = string.Empty;

        RuntimeHealthValidationResult validation =
            RuntimeHealthContractPolicy.ValidateResponse(response);

        Assert.False(validation.IsValid);
        Assert.Equal(
            RuntimeHealthContractErrorCodes.MissingBootIdentity,
            validation.ErrorCode);
    }

    [Fact]
    public void Unknown_enum_value_fails_safely()
    {
        GetRuntimeHealthResponse response = CreateResponse();
        response.Health.OverallHealth = (RuntimeHealthState)999;

        RuntimeHealthValidationResult validation =
            RuntimeHealthContractPolicy.ValidateResponse(response);

        Assert.False(validation.IsValid);
        Assert.Equal(
            RuntimeHealthContractErrorCodes.InvalidHealthState,
            validation.ErrorCode);
    }

    [Fact]
    public void Service_summaries_are_bounded()
    {
        GetRuntimeHealthResponse response = CreateResponse();
        response.Health.Services.Clear();

        for (int index = 0;
             index <= RuntimeHealthContractPolicy.MaximumServiceSummaries;
             index++)
        {
            response.Health.Services.Add(
                new ServiceHealthSummary
                {
                    ServiceId = $"runtime.service{index}",
                    State = ServiceHealthState.Ready,
                    RequiredForReadiness = false
                });
        }

        RuntimeHealthValidationResult validation =
            RuntimeHealthContractPolicy.ValidateResponse(response);

        Assert.False(validation.IsValid);
        Assert.Equal(
            RuntimeHealthContractErrorCodes.TooManyServiceSummaries,
            validation.ErrorCode);
    }

    [Fact]
    public void Oversized_request_is_rejected_before_field_validation()
    {
        GetRuntimeHealthRequest request = CreateRequest();
        request.CorrelationId = new string(
            'd',
            RuntimeHealthContractPolicy.MaximumRequestBytes);

        RuntimeHealthValidationResult validation =
            RuntimeHealthContractPolicy.ValidateRequest(request);

        Assert.False(validation.IsValid);
        Assert.Equal(
            RuntimeHealthContractErrorCodes.MessageTooLarge,
            validation.ErrorCode);
    }

    [Fact]
    public void Oversized_response_is_rejected_before_field_validation()
    {
        GetRuntimeHealthResponse response = CreateResponse();
        response.Health.Services[0].SafeDetail = new string(
            'e',
            RuntimeHealthContractPolicy.MaximumResponseBytes);

        RuntimeHealthValidationResult validation =
            RuntimeHealthContractPolicy.ValidateResponse(response);

        Assert.False(validation.IsValid);
        Assert.Equal(
            RuntimeHealthContractErrorCodes.MessageTooLarge,
            validation.ErrorCode);
    }

    [Theory]
    [InlineData(1U, 1U, 1U)]
    [InlineData(1U, 2U, 1U)]
    [InlineData(2U, 3U, 0U)]
    [InlineData(0U, 1U, 0U)]
    [InlineData(2U, 1U, 0U)]
    public void Revision_negotiation_requires_an_explicit_overlap(
        uint minimum,
        uint maximum,
        uint expected)
    {
        Assert.Equal(
            expected,
            RuntimeHealthContractPolicy.NegotiateRevision(minimum, maximum));
    }

    [Fact]
    public void Contract_limits_are_explicit_and_bounded()
    {
        Assert.Equal(
            TimeSpan.FromSeconds(2),
            RuntimeHealthContractPolicy.DefaultDeadline);
        Assert.Equal(4 * 1024, RuntimeHealthContractPolicy.MaximumRequestBytes);
        Assert.Equal(64 * 1024, RuntimeHealthContractPolicy.MaximumResponseBytes);
        Assert.Equal(64, RuntimeHealthContractPolicy.MaximumServiceSummaries);
    }

    [Fact]
    public void Unsupported_revision_returns_stable_actionable_error()
    {
        GetRuntimeHealthRequest request = CreateRequest();
        request.MinimumContractRevision = 2;
        request.MaximumContractRevision = 3;

        RuntimeHealthValidationResult requestValidation =
            RuntimeHealthContractPolicy.ValidateRequest(request);
        GetRuntimeHealthResponse response =
            RuntimeHealthContractPolicy.CreateIncompatibleRevisionResponse();
        RuntimeHealthValidationResult responseValidation =
            RuntimeHealthContractPolicy.ValidateResponse(response);

        Assert.False(requestValidation.IsValid);
        Assert.Equal(
            RuntimeHealthContractErrorCodes.IncompatibleContract,
            requestValidation.ErrorCode);
        Assert.True(responseValidation.IsValid);
        Assert.Equal(
            RuntimeHealthErrorCategory.IncompatibleContract,
            response.Error.Category);
        Assert.Equal(
            RuntimeHealthContractErrorCodes.IncompatibleContract,
            response.Error.Code);
        Assert.False(response.Error.Retryable);
    }

    [Fact]
    public void Stable_error_categories_keep_their_wire_values()
    {
        Assert.Equal(1, (int)RuntimeHealthErrorCategory.InvalidRequest);
        Assert.Equal(2, (int)RuntimeHealthErrorCategory.IncompatibleContract);
        Assert.Equal(3, (int)RuntimeHealthErrorCategory.Unavailable);
        Assert.Equal(4, (int)RuntimeHealthErrorCategory.Internal);
    }

    [Fact]
    public void Golden_request_and_response_bytes_are_stable()
    {
        Assert.Equal(
            ReadFixture("runtime-health-request-v1.hex"),
            Convert.ToHexString(CreateRequest().ToByteArray()).ToLowerInvariant());
        Assert.Equal(
            ReadFixture("runtime-health-response-v1.hex"),
            Convert.ToHexString(CreateResponse().ToByteArray()).ToLowerInvariant());
    }

    private static GetRuntimeHealthRequest CreateRequest()
    {
        return new GetRuntimeHealthRequest
        {
            MinimumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            MaximumContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            QueryId = QueryId,
            CorrelationId = CorrelationId
        };
    }

    private static GetRuntimeHealthResponse CreateResponse()
    {
        RuntimeHealthProjection projection = new()
        {
            ProductVersion = "0.1.0-test",
            RuntimeBootId = BootId,
            RuntimeMode = RuntimeMode.Normal,
            Readiness = RuntimeReadiness.Ready,
            OverallHealth = RuntimeHealthState.Healthy,
            GeneratedUnixTimeMilliseconds = 1
        };

        projection.Services.Add(
            new ServiceHealthSummary
            {
                ServiceId = "runtime.kernel",
                State = ServiceHealthState.Ready,
                RequiredForReadiness = true
            });

        return new GetRuntimeHealthResponse
        {
            ContractRevision = RuntimeHealthContractPolicy.CurrentRevision,
            Health = projection
        };
    }

    private static string ReadFixture(string fileName)
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            fileName);

        return File.ReadAllText(path).Trim();
    }
}
