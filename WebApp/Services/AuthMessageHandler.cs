using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace WebApp.Services
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public AuthMessageHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            // Atribui um HttpClientHandler padrão ao InnerHandler
            this.InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            request.Headers.Authorization = string.IsNullOrEmpty(token)
                ? null : new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
