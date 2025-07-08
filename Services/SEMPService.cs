using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SolaceWebClient.Services
{
    public class SEMPService
    {
        private readonly ILogger<SEMPService> _logger;

        public class GetQueuesDetails
        {
            public string QueueName { get; set; }
            public string QueueOwner { get; set; }
        }

        public SEMPService(ILogger<SEMPService> logger)
        {
            _logger = logger;
        }

        public async Task<List<GetQueuesDetails>> GetQueuesAsync(string url, string messageVpn, string username, string password, bool sslVerify)
        {
            var getQueuesList = new List<GetQueuesDetails>();
            string msgVpnsUrl = $"{url}/SEMP/v2/config/msgVpns";
            string queuesUrl = $"{url}/SEMP/v2/config/msgVpns/{messageVpn}/queues?count=100";

            var handler = new HttpClientHandler();
            if (!sslVerify)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            using (var client = new HttpClient(handler))
            {
                var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
                var msgVpnsRequest = new HttpRequestMessage(HttpMethod.Get, msgVpnsUrl);
                msgVpnsRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

                HttpResponseMessage msgVpnsResponse;
                try
                {
                    msgVpnsResponse = await client.SendAsync(msgVpnsRequest);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError($"Request error: {e.Message}");
                    throw;
                }
                msgVpnsResponse.EnsureSuccessStatusCode();
                var msgVpnsResponseBody = await msgVpnsResponse.Content.ReadAsStringAsync();

                bool msgVpnExist = false;

                using (JsonDocument doc = JsonDocument.Parse(msgVpnsResponseBody))
                {
                    foreach (var element in doc.RootElement.GetProperty("data").EnumerateArray())
                    {
                        if (element.GetProperty("msgVpnName").GetString() == messageVpn)
                        {
                            msgVpnExist = true;
                            break;
                        }
                    }
                }

                if (!msgVpnExist)
                {
                    _logger.LogError($"Request error: {messageVpn} does not exist");
                    throw new Exception($"Request error: MessageVPN \"{messageVpn}\" does not exist");
                }


                while (!string.IsNullOrEmpty(queuesUrl))
                {
                    var queuesRequest = new HttpRequestMessage(HttpMethod.Get, queuesUrl);
                    queuesRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

                    HttpResponseMessage queuesResponse;
                    try
                    {
                        queuesResponse = await client.SendAsync(queuesRequest);
                    }
                    catch (HttpRequestException e)
                    {
                        _logger.LogError($"Request error: {e.Message}");
                        throw;
                    }

                    queuesResponse.EnsureSuccessStatusCode();

                    var queuesResponseBody = await queuesResponse.Content.ReadAsStringAsync();

                    using (JsonDocument doc = JsonDocument.Parse(queuesResponseBody))
                    {
                        foreach (var element in doc.RootElement.GetProperty("data").EnumerateArray())
                        {
                            getQueuesList.Add(new GetQueuesDetails
                            {
                                QueueName = element.GetProperty("queueName").GetString(),
                                QueueOwner = element.GetProperty("owner").GetString()
                            });
                        }

                        if (doc.RootElement.TryGetProperty("meta", out JsonElement metaElement) &&
                            metaElement.TryGetProperty("paging", out JsonElement pagingElement) &&
                            pagingElement.TryGetProperty("nextPageUri", out JsonElement nextPageUriElement))
                        {
                            queuesUrl = nextPageUriElement.GetString();
                        }
                        else
                        {
                            queuesUrl = null;
                        }
                    }
                }
            }

            return getQueuesList;
        }

        public class ListenerPorts
        {
            public int SmfPort { get; set; }
            public int SmfsPort { get; set; }
            public bool SmfsEnabled { get; set; }
        }

        public async Task<ListenerPorts> GetListenerAsync(string url, string messageVpn, string username, string password, bool sslVerify)
        {
            string requestUrl = $"{url}/SEMP/v2/config";

            var handler = new HttpClientHandler();
            if (!sslVerify)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

                HttpResponseMessage response;
                try
                {
                    response = await client.SendAsync(request);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError($"Request error: {e.Message}");
                    throw;
                }

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                ListenerPorts config = new ListenerPorts();

                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    var msgVpnElement = doc.RootElement.GetProperty("data");
                    config.SmfPort = msgVpnElement.GetProperty("serviceSmfPlainTextListenPort").GetInt32();
                    config.SmfsPort = msgVpnElement.GetProperty("serviceSmfTlsListenPort").GetInt32();
                    config.SmfsEnabled = msgVpnElement.GetProperty("serviceSempTlsEnabled").GetBoolean();
                }
                return config;
            }
        }
    }
}
