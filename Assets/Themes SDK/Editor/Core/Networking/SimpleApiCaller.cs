using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace OpenUp.Networking
{
    public sealed class SimpleApiCaller : IDisposable
    {
        private readonly HttpClient   client = new HttpClient();
        private readonly string       baseAddress;

        public SimpleApiCaller(string baseAddress)
        {
            this.baseAddress = baseAddress;
        }
        
        /// <summary>
        /// Sets a value for the bearer credentials used to authenticate requests to the server.
        /// </summary>
        /// <param name="credentials"></param>
        public void ConfigureAuthentication(string credentials)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials);
        }

        /// <summary>
        /// Gets data from the API endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to call without server address, e.g. "sessions/invites"</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> GetAsync<[MeansImplicitUse] T>(string endpoint) where T : class
        {
            HttpResponseMessage response = await client.GetAsync($"{baseAddress}{endpoint}");

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            
            return DeserializeContent<T>(responseJson); 
        }

        public async Task<T> PostAsync<[MeansImplicitUse] T>(string endpoint, object content) where T : class
        {
            string json = SerializeContent(content);

            HttpContent         httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"{baseAddress}{endpoint}", httpContent);

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            
            return DeserializeContent<T>(responseJson);
        }
        
        public async Task PostAsync(string endpoint, object content)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(content);

            HttpContent         httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"{baseAddress}{endpoint}", httpContent);

            response.EnsureSuccessStatusCode();
        }

        public async Task<T> PutAsync<[MeansImplicitUse] T>(string endpoint, object content) where T : class
        {
            string json = SerializeContent(content ?? "");

            HttpContent         httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PutAsync($"{baseAddress}{endpoint}", httpContent);

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            
            return DeserializeContent<T>(responseJson);
        }
        
        public async Task PutAsync(string endpoint, object content)
        {
            string json = SerializeContent(content ?? "");

            HttpContent         httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PutAsync($"{baseAddress}{endpoint}", httpContent);

            response.EnsureSuccessStatusCode();
        }

        private static string SerializeContent(object content)
        {
            switch (content)
            {
                case string stringContent: return stringContent;
                default: return Newtonsoft.Json.JsonConvert.SerializeObject(content);
            }
        }

        private static T DeserializeContent<[MeansImplicitUse] T>(string content) where T : class
        {
            if (typeof(T) == typeof(string)) return content as T;
            
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(content);
        }
        
        /// <summary>
        /// Cleans up the HttpClient
        /// </summary>
        public void Dispose()
        {
            client.Dispose();
        }
    }
}
