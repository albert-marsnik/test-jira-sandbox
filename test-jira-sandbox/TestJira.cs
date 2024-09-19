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
        private readonly IJiraService _jiraService;

        public TestJira(ILogger<TestJira> logger, IJsonDataService jsonDataService, IJiraService jiraService)
        {
            _logger = logger;
            _jsonDataService = jsonDataService;
            _jiraService = jiraService;
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
                try
                {
                    await _jiraService.EnsureCustomFieldsExist(jiraPayload.Fields?.CustomFields);
                    var result = await _jiraService.CreateJiraTicket(jiraPayload, jsonSerializerSettings);
                    return new OkObjectResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing Jira request: {ex.Message}");
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
