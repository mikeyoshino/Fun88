namespace Fun88.Web.Infrastructure.Clients;

public class OpenAiHttpClient(HttpClient httpClient)
{
    public HttpClient Client => httpClient;
}
