using System.Net;

namespace Tests;

[TestClass]
public sealed class InfoEndpointTests
{
    [TestMethod]
    public async Task GetInfo_ReturnsOkWithEmptyBody()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/v1/info");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [TestMethod]
    public async Task GetUnknownPath_ReturnsNotFound()
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("/v1/info/not-found");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task PostInfo_ReturnsMethodNotAllowed()
    {
        using var client = CreateClient();
        using var response = await client.PostAsync("/v1/info", null);

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not-a-url")]
    [DataRow("ftp://127.0.0.1:5000")]
    [DataRow("http://127.0.0.1:5000/path")]
    [DataRow("http://127.0.0.1:5000/?query=value")]
    [DataRow("http://user:password@127.0.0.1:5000")]
    public void InvalidTestBaseUrl_ThrowsClearError(string configuredAddress)
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ParseBaseAddress(configuredAddress);
        });

        StringAssert.Contains(exception.Message, "DOCUMENT_SEARCH_TEST_BASE_URL");
    }

    private static HttpClient CreateClient()
    {
        var configuredAddress = Environment.GetEnvironmentVariable("DOCUMENT_SEARCH_TEST_BASE_URL")
            ?? "http://127.0.0.1:5000";
        var baseAddress = ParseBaseAddress(configuredAddress);

        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private static Uri ParseBaseAddress(string configuredAddress)
    {
        if (!Uri.TryCreate(configuredAddress, UriKind.Absolute, out var baseAddress)
            || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps)
            || baseAddress.AbsolutePath != "/"
            || baseAddress.Query.Length != 0
            || baseAddress.Fragment.Length != 0
            || baseAddress.UserInfo.Length != 0)
        {
            throw new InvalidOperationException(
                "DOCUMENT_SEARCH_TEST_BASE_URL must be an HTTP or HTTPS origin, such as http://127.0.0.1:5000.");
        }

        return baseAddress;
    }
}
