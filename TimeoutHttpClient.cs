using System;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Azure.SignalR.Samples.Serverless
{
    public class TimeoutHttpClient
    {
        public static HttpClient GenerateHttpClient(int seconds=10)
        {
            var handler = new TimeoutHandler
            {
                DefaultTimeout = TimeSpan.FromSeconds(seconds),
                InnerHandler = new HttpClientHandler()
            };
            var client = new HttpClient(handler);
            client.Timeout = Timeout.InfiniteTimeSpan;
            return client;
        }
    }
}
