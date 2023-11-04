namespace WebApiPollyExample
{
    public interface IApiService
    {
        Task<HttpResponseMessage> CallApi();
    }

    public class ApiService : IApiService
    {
        private readonly IHttpClientFactory _clientFactory;

        public ApiService(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<HttpResponseMessage> CallApi()
        {
            var httpClient = _clientFactory.CreateClient("ResilientClient");
            return await httpClient.GetAsync("https://google.com/asdf");
        }
    }
}
