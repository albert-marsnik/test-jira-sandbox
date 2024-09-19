using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using test_jira_sandbox.Models;
using test_jira_sandbox.Services;

namespace test_jira_sandbox
{
    public class TestJira
    {
        private readonly ILogger<TestJira> _logger;
        private readonly IJsonDataService _jsonDataService;

        public TestJira(ILogger<TestJira> logger, IJsonDataService jsonDataService)
        {
            _logger = logger;
            _jsonDataService = jsonDataService;
        }

        private static readonly string? JIRA_URL = Environment.GetEnvironmentVariable("JIRA_URL");
        private static readonly string? JIRA_USER = Environment.GetEnvironmentVariable("JIRA_USER");
        private static readonly string? JIRA_API_TOKEN = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");

        [Function(nameof(TestJira))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest? req = null)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string jsonData;

            try
            {
                jsonData = _jsonDataService.GetJsonData();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving JSON data: {ex.Message}");
                return new UnprocessableEntityObjectResult(ex);
            }

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                Converters = { new JiraAdfDescriptionConverter() },
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var jiraPayload = new JiraPayload(jsonData, jsonSerializerSettings);

            if (jiraPayload.IsValidJiraPayload())
            {
                string jiraApiUrl = $"{JIRA_URL}/rest/api/3/issue";
                var client = new HttpClient();
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{JIRA_USER}:{JIRA_API_TOKEN}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
                var serializedJiraPayload = JsonConvert.SerializeObject(jiraPayload, jsonSerializerSettings);
                var content = new StringContent(serializedJiraPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage? response = null;

                try
                {
                    response = await client.PostAsync(jiraApiUrl, content);
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult($"Jira ticket created successfully: {await response.Content.ReadAsStringAsync()}");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError($"Error creating Jira ticket: {ex.Message}");
                    if (response != null)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Jira API response: {errorContent}");

                        return new BadRequestObjectResult(errorContent);
                    }

                    return new UnprocessableEntityObjectResult(ex);
                }
            }
            else
            {
                return new BadRequestObjectResult($"Failed to create Jira ticket, {jiraPayload.ReasonForInvalidJiraPayload()}");
            }
        }
    }
}
