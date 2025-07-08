using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SolaceSystems.Solclient.Messaging;
using System.Text.Json;

namespace SolaceWebClient.Services
{
    public class OAuth2
    {
        private readonly HttpClient client;

        public OAuth2()
        {
            client = new HttpClient();
        }

        public string GetToken(Authentication auth)
        {
            if (auth.Scheme == AuthenticationSchemes.OAUTH2)
            {
                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", auth.ClientId),
                    new KeyValuePair<string, string>("client_secret", auth.ClientSecret),
                    new KeyValuePair<string, string>("scope", auth.Scope),
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                });

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                try
                {
                    var task = Task.Run(async () =>
                    {
                        var response = await client.PostAsync(auth.IssuerUri, requestBody);
                        response.EnsureSuccessStatusCode();

                        var responseBody = await response.Content.ReadAsStringAsync();
                        var jsonDocument = JsonDocument.Parse(responseBody);
                        return jsonDocument.RootElement.GetProperty("access_token").GetString();
                    });
                    return task.GetAwaiter().GetResult();
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                }
            }
            return "";
        }
    }
}