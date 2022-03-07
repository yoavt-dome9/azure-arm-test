using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Dome9.CloudGuardOnboarding.Azure;


namespace FunctionApp1
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function '{nameof(Function1)}' got request.");

            string name = req.Query["name"];

            log.LogInformation($"request parameter name={name}");
            log.LogInformation($"HttpRequest.ContentLength={req.ContentLength}");
            log.LogInformation($"HttpRequest='{req}'");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"parsed string from stream, requestBody={requestBody}");

            dynamic data = JsonConvert.DeserializeObject(requestBody);

            name = name ?? data?.name;
            log.LogInformation($"parsed body dynamic property or if null request param 'name'={name}");

            try
            {
                log.LogInformation($"starting typed OnboardingFunctionRequest model creation");

                OnboardingFunctionRequest onboardingFunctionRequest = new OnboardingFunctionRequest
                {
                    ApiKey = data.ApiKey,
                    ApiBaseUrl = data.ApiBaseUrl,
                    Secret = data.Secret,
                    OnboardingId = data.OnboardingId,
                    Message = data.Message,
                };

                log.LogInformation($"passed OnboardingFunctionRequest model creation. {nameof(onboardingFunctionRequest.Message)}='{onboardingFunctionRequest.Message}'");

                await UpdateStatus(onboardingFunctionRequest, log);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }


            HttpResponseMessage resp = null;
            try
            {
                // just do something that involves an external api call - e.g. webhook 
                var webhook = $"https://webhook.site/f3afd92d-413e-4c84-81b0-16af5c7176ea?name={name}";
                HttpClient httpClient = new HttpClient();
                resp = await httpClient.GetAsync(webhook);
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }


            string responseMessage = string.IsNullOrEmpty(name) ?
                $"This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response. And by the way, the {nameof(resp.StatusCode)}='{resp?.StatusCode}'"
                : $"Hello, '{name}'. This HTTP triggered function executed successfully. And by the way, the {nameof(resp.StatusCode)}='{resp?.StatusCode}'";

            return new OkObjectResult(responseMessage);
        }

        private async static Task UpdateStatus(OnboardingFunctionRequest model, ILogger log)
        {
            log.LogInformation($"about to create statusModel");
            StatusModel statusModel = new StatusModel
            {
                Action = "Create",
                Feature = "None",
                OnboardingId = model.OnboardingId,
                Message = model.Message,
            };
            log.LogInformation($"passed statusModel={statusModel}");


            log.LogInformation($"about to create serviceAccount");
            ServiceAccount serviceAccount = new ServiceAccount(model.ApiKey, model.Secret, model.ApiBaseUrl);
            log.LogInformation($"passed serviceAccount={serviceAccount}");


            try
            {
                log.LogInformation($"about to post status to CloudGuard API");
                var api = new ApiWrapper();
                api.SetLocalCredentials(serviceAccount);
                await api.UpdateOnboardingStatus(statusModel, log);
                log.LogInformation($"posted status to CloudGuard API");
            }
            catch (Exception ex)
            {
                log.LogError($"failed to post status to CloudGuard API");
                log.LogError(ex.ToString());
            }
        }
    }
}
