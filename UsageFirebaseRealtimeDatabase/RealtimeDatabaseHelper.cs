using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UsageFirebaseRealtimeDatabase
{
    public class RealtimeDatabaseHelper
    {
        private const string userAgent = "dudu-v1";

        FirebaseAdmin.Auth.FirebaseAuth _auth;
        FirebaseApp _app;
        GoogleCredential _googleCredential;

        string _accessToken;
        string _customToken;
        string _fileJson;

        string _rootUrl;

        public RealtimeDatabaseHelper()
        {
            //https://console.cloud.google.com/iam-admin/serviceaccounts?project=test-316a0&supportedpurview=project get account services id
            // will create test-316a0-dd5a88c8c233.json , should download and config to get it
            //https://www.c-sharpcorner.com/article/retrieve-access-token-for-google-service-account-form-json-or-p12-key-in-c-sharp/

            //https://firebase.google.com/docs/database/rest/auth

            _fileJson = ConfigurationManager.AppSettings["GoogleCredentialFileName"];
            _rootUrl = ConfigurationManager.AppSettings["GoogleCredentialRealtimeDbRootUrl"];

            string fileCredential = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _fileJson);

            if (File.Exists(fileCredential) == false)
            {
                throw new Exception("Not existed file json for GoogleCredential");
            }

            _googleCredential = GoogleCredential.FromFile(fileCredential)
                            .CreateScoped(
                                new[] {
                                        "https://www.googleapis.com/auth/firebase.database",
                                        "https://www.googleapis.com/auth/userinfo.email",
                                        "https://www.googleapis.com/auth/firebase",
                                        "https://www.googleapis.com/auth/cloud-platform"
                                    }
                                );

            _app = FirebaseApp.Create(new AppOptions
            {
                Credential = _googleCredential
            });

            GetAccessTokenForRequestAsync().GetAwaiter().GetResult();
        }

        public async Task<string> GetAccessTokenForRequestAsync()
        {
            ITokenAccess c = _googleCredential as ITokenAccess;
            _accessToken = await c.GetAccessTokenForRequestAsync();

            return _accessToken;
        }

        public async Task<string> CreateCustomTokenAsync(string userId, Dictionary<string, object> claims)
        {
            claims = claims ?? new Dictionary<string, object>();
            claims["userId"] = userId;

            _customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(userId, claims);

            return _customToken;
        }
        public async Task<string> Delete(string dbUrlRef, string customToken = "")
        {
            dbUrlRef = dbUrlRef.Trim('/');
            var rootUrl = _rootUrl.Trim('/');

            var realUrl = $"{rootUrl}/{dbUrlRef}/.json?access_token={_accessToken}";

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(realUrl);
                httpClient.DefaultRequestHeaders.Add("user-agent", userAgent);

                if (!string.IsNullOrEmpty(customToken))
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {customToken}");

                var msg = new HttpRequestMessage(HttpMethod.Delete, realUrl);
                msg.Headers.Add("user-agent", userAgent);

                if (!string.IsNullOrEmpty(customToken))
                    msg.Headers.Add("Authorization", $"Bearer {customToken}");

                var res = await httpClient.SendAsync(msg);

                return await res.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> PublishPartialOfData(string dbUrlRef, object data, string customToken = "")
        {
            dbUrlRef = dbUrlRef.Trim('/');
            var rootUrl = _rootUrl.Trim('/');

            var realUrl = $"{rootUrl}/{dbUrlRef}/.json?access_token={_accessToken}";
            string json = System.Text.Json.JsonSerializer.Serialize(data);

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(realUrl);
                httpClient.DefaultRequestHeaders.Add("user-agent", userAgent);

                if (!string.IsNullOrEmpty(customToken))
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {customToken}");

                var msg = new HttpRequestMessage(HttpMethod.Patch, realUrl);
                msg.Headers.Add("user-agent", userAgent);

                if (!string.IsNullOrEmpty(customToken))
                    msg.Headers.Add("Authorization", $"Bearer {customToken}");

                msg.Content = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

                var res = await httpClient.SendAsync(msg);


                return await res.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> Publish<T>(string dbUrlRef, T data, string customToken = "") where T : class
        {
            dbUrlRef = dbUrlRef.Trim('/');
            var rootUrl = _rootUrl.Trim('/');

            var realUrl = $"{rootUrl}/{dbUrlRef}/.json?access_token={_accessToken}";

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(realUrl);

                string json = System.Text.Json.JsonSerializer.Serialize(data);

                httpClient.DefaultRequestHeaders.Add("user-agent", userAgent);

                if (!string.IsNullOrEmpty(customToken))
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {customToken}");

                var msg = new HttpRequestMessage(HttpMethod.Put, realUrl);
                msg.Headers.Add("user-agent", userAgent);

                if (!string.IsNullOrEmpty(customToken))
                    msg.Headers.Add("Authorization", $"Bearer {customToken}");

                msg.Content = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

                var res = await httpClient.SendAsync(msg);

                var resBody = await res.Content.ReadAsStringAsync();

                return resBody;
            }
        }

        public async Task<string> Get(string dbUrlRef, string customToken = "")
        {
            dbUrlRef = dbUrlRef.Trim('/');
            var rootUrl = _rootUrl.Trim('/');

            var realUrl = $"{rootUrl}/{dbUrlRef}/.json?access_token={_accessToken}";

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(realUrl);
                httpClient.DefaultRequestHeaders.Add("user-agent", userAgent);

                if (!string.IsNullOrEmpty(customToken))
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {customToken}");

                var res = await httpClient.GetAsync(realUrl);

                return await res.Content.ReadAsStringAsync();
            }
        }

        public async Task Subscribe(string dbUrlRef, Func<string, string, string, string, Task> callBack, string customToken = "")
        {
            await Task.Factory.StartNew(async () =>
            {
                try
                {
                    dbUrlRef = dbUrlRef.Trim('/');
                    var rootUrl = _rootUrl.Trim('/');
                    var fullUrl = $"{rootUrl}/{dbUrlRef}/";
                    var realUrl = $"{rootUrl}/{dbUrlRef}/.json?access_token={_accessToken}";

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.BaseAddress = new Uri(realUrl);
                        httpClient.DefaultRequestHeaders.Add("user-agent", userAgent);
                        httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream");

                        if (!string.IsNullOrEmpty(customToken))
                            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {customToken}");

                        var request = new HttpRequestMessage(HttpMethod.Get, realUrl);
                        request.Headers.Add("user-agent", userAgent);
                        request.Headers.Add("accept", "text/event-stream");

                        if (!string.IsNullOrEmpty(customToken))
                            request.Headers.Add("Authorization", $"Bearer {customToken}");

                        using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                        {
                            using (var body = await response.Content.ReadAsStreamAsync())
                            {
                                using (var reader = new StreamReader(body))
                                {
                                    List<string> receivedData = new List<string>();
                                    while (!reader.EndOfStream)
                                    {
                                        try
                                        {
                                            var msg = await reader.ReadLineAsync();

                                            if (string.IsNullOrEmpty(msg) || string.IsNullOrWhiteSpace(msg)) continue;

                                            receivedData.Add(msg);

                                            if (receivedData.Count == 2)
                                            {
                                                await callBack(receivedData.FirstOrDefault(i => i.StartsWith("event"))
                                                    , receivedData.FirstOrDefault(i => !i.StartsWith("event")), dbUrlRef, fullUrl);

                                                receivedData.Clear();
                                            }
                                        }
                                        finally
                                        {
                                            await Task.Delay(10);
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                catch (TimeoutException exTimeout)
                {
                    Console.WriteLine("Timeout exception: " + exTimeout.Message);
                    await Subscribe(dbUrlRef, callBack, customToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }, TaskCreationOptions.LongRunning);

        }
    }
}
