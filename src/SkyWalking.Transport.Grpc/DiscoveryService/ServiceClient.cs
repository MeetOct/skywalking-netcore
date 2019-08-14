using Newtonsoft.Json;
using SkyWalking.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SkyWalking.Transport.Grpc.DiscoveryService
{
    public class ServiceClient : IDisposable
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public ServiceClient(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(ServiceClient));
            _httpClient = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(5000) };
        }

        public async Task<ServiceResponse<T>> GetAsync<T>(string url,int timeout)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            try
            {
                using (var cts = new CancellationTokenSource(timeout))
                {
                    var response = await _httpClient.SendAsync(request, cts.Token);
                    var result = new ServiceResponse<T>() { HttpStatus = response.StatusCode };
                    if (response.StatusCode == HttpStatusCode.OK|| response.StatusCode == HttpStatusCode.NotModified)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        result.Body = JsonConvert.DeserializeObject<T>(content);
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"http error,url{url}", ex);
            }
            return new ServiceResponse<T>() { HttpStatus = HttpStatusCode.InternalServerError };
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }
    }
}
