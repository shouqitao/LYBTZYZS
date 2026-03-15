using System.Net;
using System.Net.Http;
using LYBT.Desktop.Sync.Services;
using LYBT.Desktop.Sync.ViewModels;
using Refit;

namespace LYBT.Tests.Desktop.PureLogic.Sync;

public class SyncErrorClassifierTests
{
    #region Classify Tests - Basic Exceptions

    [Fact]
    public void Classify_HttpRequestException_ReturnsTransientNetwork()
    {
        var ex = new HttpRequestException("Connection failed");

        var result = SyncErrorClassifier.Classify(ex);

        Assert.Equal(SyncErrorCategory.TransientNetwork, result);
    }

    [Fact]
    public void Classify_TaskCanceledException_ReturnsTransientNetwork()
    {
        var ex = new TaskCanceledException("Request timeout");

        var result = SyncErrorClassifier.Classify(ex);

        Assert.Equal(SyncErrorCategory.TransientNetwork, result);
    }

    [Fact]
    public void Classify_GenericException_ReturnsUnknown()
    {
        var ex = new InvalidOperationException("Something went wrong");

        var result = SyncErrorClassifier.Classify(ex);

        Assert.Equal(SyncErrorCategory.Unknown, result);
    }

    #endregion

    #region Classify Tests - ApiException (using Refit factory)

    [Fact]
    public async Task Classify_ApiException_Unauthorized_ReturnsAuthExpired()
    {
        var ex = await CreateApiExceptionAsync(HttpStatusCode.Unauthorized);

        var result = SyncErrorClassifier.Classify(ex);

        Assert.Equal(SyncErrorCategory.AuthExpired, result);
    }

    [Fact]
    public async Task Classify_ApiException_Conflict_ReturnsConflictChanged()
    {
        var ex = await CreateApiExceptionAsync(HttpStatusCode.Conflict);

        var result = SyncErrorClassifier.Classify(ex);

        Assert.Equal(SyncErrorCategory.ConflictChanged, result);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task Classify_ApiException_ClientError_ReturnsBusinessReject(HttpStatusCode statusCode)
    {
        var ex = await CreateApiExceptionAsync(statusCode);

        var result = SyncErrorClassifier.Classify(ex);

        Assert.Equal(SyncErrorCategory.BusinessReject, result);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Classify_ApiException_ServerError_ReturnsUnknown(HttpStatusCode statusCode)
    {
        var ex = await CreateApiExceptionAsync(statusCode);

        var result = SyncErrorClassifier.Classify(ex);

        Assert.Equal(SyncErrorCategory.Unknown, result);
    }

    #endregion

    #region IsRetryable Tests

    [Theory]
    [InlineData(SyncErrorCategory.TransientNetwork, true)]
    [InlineData(SyncErrorCategory.AuthExpired, true)]
    [InlineData(SyncErrorCategory.ConflictChanged, true)]
    [InlineData(SyncErrorCategory.BusinessReject, false)]
    [InlineData(SyncErrorCategory.Unknown, false)]
    public void IsRetryable_ReturnsExpectedResult(SyncErrorCategory category, bool expected)
    {
        var result = SyncErrorClassifier.IsRetryable(category);

        Assert.Equal(expected, result);
    }

    #endregion

    #region Helper Methods

    private static async Task<ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode)
    {
        // Refit 8.0 Create method returns Task<ApiException>
        var request = new HttpRequestMessage(HttpMethod.Get, "http://test");
        var response = new HttpResponseMessage(statusCode);

        // Use the public factory method from Refit 8.0
        return await ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
    }

    #endregion
}
