namespace Tests;

internal static class TestApiClient
{
    public static HttpClient CreateClient(TimeSpan? timeout = null)
    {
        var configuredAddress = Environment.GetEnvironmentVariable("DOCUMENT_SEARCH_TEST_BASE_URL")
            ?? "http://127.0.0.1:5000";
        var baseAddress = ParseBaseAddress(configuredAddress);

        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = baseAddress,
            Timeout = timeout ?? TimeSpan.FromSeconds(10)
        };
    }

    public static Uri ParseBaseAddress(string configuredAddress)
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
