/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using SkyWalking.Config;
using SkyWalking.Logging;
using SkyWalking.Transport.Grpc.DiscoveryService;

namespace SkyWalking.Transport.Grpc
{
    public class ConnectionManager
    {
        private readonly Random _random = new Random();
        private readonly AsyncLock _lock = new AsyncLock();

        private readonly ILogger _logger;
        private readonly GrpcConfig _config;
        private readonly ServiceConfigLocator _configLocator;

        private volatile Channel _channel;
        private volatile ConnectionState _state;
        private volatile string _server;

        public bool Ready => _channel != null && _state == ConnectionState.Connected && _channel.State == ChannelState.Ready;

        public ConnectionManager(ILoggerFactory loggerFactory, IConfigAccessor configAccessor, ServiceConfigLocator configLocator)
        {
            _configLocator = configLocator;
            _logger = loggerFactory.CreateLogger(typeof(ConnectionManager));
            _config = configAccessor.Get<GrpcConfig>();
        }

        public async Task ConnectAsync()
        {
            using (await _lock.LockAsync())
            {
                if (Ready)
                {
                    return;
                }

                if (_channel != null)
                {
                    await ShutdownAsync();
                }

                await EnsureServerAddress();

                if (string.IsNullOrWhiteSpace(_server))
                {
                    _logger.Information($"Not found available gRPC connection servcer");
                    _state = ConnectionState.Failure;
                    return;
                }

                _channel = new Channel(_server, ChannelCredentials.Insecure);

                try
                {
                    var deadLine = DateTime.UtcNow.AddMilliseconds(_config.ConnectTimeout);
                    await _channel.ConnectAsync(deadLine);
                    _state = ConnectionState.Connected;
                    _logger.Information($"Connected collector server[{_channel.Target}].");
                }
                catch (TaskCanceledException ex)
                {
                    _state = ConnectionState.Failure;
                    _logger.Error($"Connect collector timeout.", ex);
                }
                catch (Exception ex)
                {
                    _state = ConnectionState.Failure;
                    _logger.Error($"Connect collector fail.", ex);
                }
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                await _channel?.ShutdownAsync();
                _logger.Information($"Shutdown connection[{_channel.Target}].");
            }
            catch (Exception e)
            {
                _logger.Error($"Shutdown connection fail.", e);
            }
            finally
            {
                _state = ConnectionState.Failure;
            }
        }

        public void Failure(Exception exception)
        {
            var currentState = _state;

            if (ConnectionState.Connected == currentState)
            {
                _logger.Warning($"Connection state changed. {_state} -> {_channel.State} . {exception.Message}");
            }

            _state = ConnectionState.Failure;
        }

        public Channel GetConnection()
        {
            if (Ready) return _channel;
            _logger.Debug("Not found available gRPC connection.");
            return null;
        }

        private async Task EnsureServerAddress()
        {
            List<ServiceDto> dtos= await _configLocator.GetServices();
            if (dtos.Any() != true)
            {
                return;
            }
            if (dtos.Count == 1)
            {
                _server = dtos[0].Url;
                return;
            }
            _server = dtos[_random.Next() % dtos.Count].Url;
        }
    }

    public enum ConnectionState
    {
        Idle,
        Connecting,
        Connected,
        Failure
    }
}