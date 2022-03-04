using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace OnlineStore.APIAuth
{
    public class JwtAuth
    {
        private readonly string apiUrl;
        private readonly string userName;
        private readonly string password;

        public JwtAuth() 
        {
            apiUrl = ConfigurationManager.AppSettings["WebAPIurl"].ToString();
            userName = ConfigurationManager.AppSettings["APIUsername"].ToString();
            password = ConfigurationManager.AppSettings["APIPassword"].ToString();
        }

        public async Task<JwtAuthToken> JwtAuthToken()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(@"" + apiUrl + @"api/token");
                var httpContent = new HttpRequestMessage(HttpMethod.Get, @"?userName=" + userName + "&password=" + password);
                var response = client.SendAsync(httpContent).Result;
                var contents = await response.Content.ReadAsStringAsync();
                contents = contents.TrimStart('\"');
                contents = contents.TrimEnd('\"');
                contents = contents.Replace("\\", "");
                var authToken = JsonConvert.DeserializeObject<JwtAuthToken>(contents);

                return authToken;
            }
        }
    }

    public class JwtAuthToken
    {
        public string TokenType { get; set; }
        public string Token { get; set; }
        public DateTime ExpirationTime { get; set; }
    }
}