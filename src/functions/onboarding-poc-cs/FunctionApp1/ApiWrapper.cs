using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dome9.CloudGuardOnboarding.Azure
{
    internal class ApiWrapper
    {
        protected HttpClient _httpClient;

        protected const string CONTROLLER_ROUTE = "/v2/UnifiedOnboarding";
        protected const string SERVERLESS_ADD_ACCOUNT_ROUTE = "/v2/serverless/accounts";
        protected string _baseUrl;
        protected bool disposedValue;
        protected static StatusModel _lastStatus = new StatusModel();
        protected static SemaphoreSlim _semaphore = new SemaphoreSlim(1);


        public void SetLocalCredentials(ServiceAccount cloudGuardServiceAccount)
        {
            _httpClient?.Dispose();
            _httpClient = new HttpClient();

            _baseUrl = cloudGuardServiceAccount.BaseUrl;
            var authenticationString = $"{cloudGuardServiceAccount.ApiCredentials.ApiKeyId}:{cloudGuardServiceAccount.ApiCredentials.ApiKeySecret}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

            //setup reusable http client
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.ConnectionClose = true;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
        }

        public async Task UpdateOnboardingStatus(StatusModel model, ILogger log)
        {
            await _semaphore.WaitAsync();

            try
            {
                string methodRoute = "UpdateStatus";
                if (_lastStatus.Equals(model))
                {
                    log.LogInformation($"[INFO] [{nameof(UpdateOnboardingStatus)}] Status is same as previous, hence will not be post update to server.");
                    return;
                }

                log.LogInformation($"[INFO] [{nameof(UpdateOnboardingStatus)}] POST method:{methodRoute}, model:{model}");

                var response = await _httpClient.PostAsync($"{CONTROLLER_ROUTE}/{methodRoute}", HttpClientUtils.GetContent(model, HttpClientUtils.SerializationOptionsType.CamelCase));
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new Exception($"Response StatusCode:{response?.StatusCode} Reason:{response?.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"[Error] [{nameof(UpdateOnboardingStatus)} failed. Error={ex}]");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
