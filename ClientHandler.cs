// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.Azure.SignalR.Samples.Serverless
{
    public class ClientHandler
    {
        private readonly HubConnection _connection;

	    private NLog.Logger _logger;

	    public ClientHandler(string connectionString, string hubName, string userId)
        {
            _logger = NLog.LogManager.GetLogger(GetType().Name);
            var pid = Process.GetCurrentProcess().Id;
            _logger.Info($"Process ID: {pid}");
            var serviceUtils = new ServiceUtils(connectionString);

            var url = GetClientUrl(serviceUtils.Endpoint, hubName);

            _connection = new HubConnectionBuilder()
                .WithUrl(url, option =>
                {
                    option.AccessTokenProvider = () =>
                    {
                        return Task.FromResult(serviceUtils.GenerateAccessToken(url, userId));
                    };
                }).Build();

            _connection.On<string, string>("SendMessage",
                (string server, string message) =>
                {
                    _logger.Info($"[{DateTime.Now.ToString()}] Received message from server {server}: {message}");
                });
        }

        public async Task StartAsync()
        {
            await _connection.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        private string GetClientUrl(string endpoint, string hubName)
        {
            return $"{endpoint}/client/?hub={hubName}";
        }
    }
}
