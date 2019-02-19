// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Microsoft.Azure.SignalR.Samples.Serverless
{
    public class ServerHandler
    {
        private static readonly int _defaultTimeout = 7;
        private static readonly TimeSpan _reqTimeout = TimeSpan.FromSeconds(_defaultTimeout);
        private static readonly HttpClient Client = TimeoutHttpClient.GenerateHttpClient(_defaultTimeout);

        private readonly string _serverName;

        private readonly ServiceUtils _serviceUtils;

        private readonly string _hubName;

        private readonly string _endpoint;

        private readonly PayloadMessage _defaultPayloadMessage;

        private NLog.Logger _logger;

        public ServerHandler(string connectionString, string hubName)
        {
            _serverName = GenerateServerName();
            _serviceUtils = new ServiceUtils(connectionString);
            _hubName = hubName;
            _endpoint = _serviceUtils.Endpoint;

            _defaultPayloadMessage = new PayloadMessage
            {
                Target = "SendMessage",
                Arguments = new[]
                {
                    _serverName,
                    "Hello from server",
                }
            };
        }

        public async Task Start()
        {
            _logger = NLog.LogManager.GetLogger(GetType().Name);
            var pid = Process.GetCurrentProcess().Id;
            _logger.Info($"Process ID: {pid}");
            ShowHelp();
            int messaegeCount = 0;
            DateTime startTime = DateTime.Now;
            while (true)
            {
                //System.Threading.Thread.Sleep(1);
                //var argLine = Console.ReadLine();
                try
                {
                    var argLine = "broadcastMessage";
                    if (argLine == null)
                    {
                        continue;
                    }
                    var args = argLine.Split(' ');

                    if (args.Length == 1 && args[0].Equals("broadcastMessage"))
                    {
                        await SendRequest(args[0], _hubName);
                        messaegeCount++;
                        _logger.Info(String.Format("Messsage {0} is sent. Start: {1}, Now: {2}", messaegeCount, startTime, DateTime.Now));
                    }
                    else if (args.Length == 3 && args[0].Equals("send"))
                    {
                        await SendRequest(args[1], _hubName, args[2]);
                    }
                    else
                    {
                        Console.WriteLine($"Can't recognize command {argLine}");
                    }
                }
                catch (Exception e)
                {
                    _logger.Error($"see exception {e.Message}");
                }
            }
        }

        public async Task SendRequest(string command, string hubName, string arg = null)
        {
            string url = null;
            switch (command)
            {
                case "user":
                    url = GetSendToUserUrl(hubName, arg);
                    break;
                case "users":
                    url = GetSendToUsersUrl(hubName, arg);
                    break;
                case "group":
                    url = GetSendToGroupUrl(hubName, arg);
                    break;
                case "groups":
                    url = GetSendToGroupsUrl(hubName, arg);
                    break;
                case "broadcastMessage":
                    url = GetBroadcastUrl(hubName);
                    break;
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    break;
            }

            if (!string.IsNullOrEmpty(url))
            {
                
                try
                {
                    using (var request = BuildRequest(url))
                    using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (response.StatusCode != HttpStatusCode.Accepted)
                        {
                            _logger.Error($"Sent error: {response.StatusCode}");
                        }
                        else
                        {
                            Console.WriteLine($"");
                        }
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.Error($"A sending task was canceled! {ex.Message}");
                }
            }
        }

        private Uri GetUrl(string baseUrl)
        {
            return new UriBuilder(baseUrl).Uri;
        }

        private string GetSendToUserUrl(string hubName, string userId)
        {
            return $"{GetBaseUrl(hubName)}/users/{userId}";
        }

        private string GetSendToUsersUrl(string hubName, string userList)
        {
            return $"{GetBaseUrl(hubName)}/users/{userList}";
        }

        private string GetSendToGroupUrl(string hubName, string group)
        {
            return $"{GetBaseUrl(hubName)}/group/{group}";
        }

        private string GetSendToGroupsUrl(string hubName, string groupList)
        {
            return $"{GetBaseUrl(hubName)}/groups/{groupList}";
        }

        private string GetBroadcastUrl(string hubName)
        {
            return $"{GetBaseUrl(hubName)}";
        }

        private string GetBaseUrl(string hubName)
        {
            return $"{_endpoint}/api/v1/hubs/{hubName.ToLower()}";
        }

        private string GenerateServerName()
        {
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }

        private HttpRequestMessage BuildRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetUrl(url));
            request.SetTimeout(_reqTimeout);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _serviceUtils.GenerateAccessToken(url, _serverName));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonConvert.SerializeObject(_defaultPayloadMessage), Encoding.UTF8, "application/json");

            return request;
        }

        private void ShowHelp()
        {
            Console.WriteLine("*********Usage*********\n" +
                              "send user <User Id>\n" +
                              "send users <User Id List>\n" +
                              "send group <Group Name>\n" +
                              "send groups <Group List>\n" +
                              "broadcastMessage\n" +
                              "***********************");
        }
    }

    public class PayloadMessage
    {
        public string Target { get; set; }

        public object[] Arguments { get; set; }
    }
}
