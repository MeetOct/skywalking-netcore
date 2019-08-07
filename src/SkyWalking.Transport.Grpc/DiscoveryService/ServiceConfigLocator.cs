using SkyWalking.Config;
using SkyWalking.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkyWalking.Transport.Grpc.DiscoveryService
{
    public class ServiceConfigLocator : IDisposable
    {
        private readonly ILogger _logger;
        private readonly DiscoveryServiceConfig _config;
        private readonly ServiceClient _client;
        private List<ServiceDto> _services;
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly Timer _timer;
        public ServiceConfigLocator(ILoggerFactory loggerFactory, IConfigAccessor configAccessor, ServiceClient serviceClient)
        {
            _client = serviceClient;
            _services = new List<ServiceDto>();
            _logger = loggerFactory.CreateLogger(typeof(ServiceConfigLocator));
            _config = configAccessor.Get<DiscoveryServiceConfig>();
            _timer = new Timer(ScheduleRefresh, null, 0, _config.RefreshInterval);
        }

        public async Task<List<ServiceDto>> GetServices()
        {
            var result = AtomicRead();
            if (result.Any() == true)
            {
                return result;
            }
            await UpdateServices();
            return AtomicRead();
        }

        private async void ScheduleRefresh(object _)
        {
            await UpdateServices();
        }

        private async Task UpdateServices()
        {
            using (await _lock.LockAsync())
            {
                await UpdateServices(3);
            }
        }

        private async Task UpdateServices(int times)
        {
            var address = GetMetaServiceUrl();
            int index = 0;
            while (index++ < times)
            {
                try
                {
                    var result = await _client.GetAsync<List<string>>(address, _config.Timeout);
                    if (result.HttpStatus == HttpStatusCode.NotModified)
                    {
                        return;
                    }
                    if (result.HttpStatus == HttpStatusCode.OK)
                    { 
                        if (result.Body?.Any() == true)
                        {
                            AtomicExchange(result.Body.Select(b=>new ServiceDto() { Url=b }).ToList());
                            return;
                        }
                        _logger.Information($"no available service config from {address}");
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.Error($"get services config failed from {address}",ex);
                }
            }
        }

        private string GetMetaServiceUrl()
        {
            return _config.ServiceAddress;
        }

        private void AtomicExchange(List<ServiceDto> dtos)
        {
            Interlocked.Exchange(ref _services, dtos);
        }

        private List<ServiceDto> AtomicRead()
        {
            var value = _services;
            Interlocked.MemoryBarrier();
            return value;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
