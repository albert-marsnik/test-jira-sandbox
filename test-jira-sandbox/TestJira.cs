using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;

namespace Sandbox
{
    public class TestJira
    {
        private readonly ILogger<TestJira> _logger;

        public TestJira(ILogger<TestJira> logger)
        {
            _logger = logger;
        }

        private static readonly string JIRA_URL = "https://ti8m-marsnik.atlassian.net";
        private static readonly string JIRA_USER = "albert.marsnik@ti8m.ch";
        private static readonly string JIRA_API_TOKEN = "ATATT3xFfGF0lC9g7wn7mdlJxFGatnHt4v5KgROR9r372CxYNgkCzLKb2gYvrhzwTq9N-jo_VBiHlB_PBoKLi_mPI3xoG9FA_MFQxubCJRKlXMJ-06b-wdVQfZ_LT9L3LtUEVq60lvi_SdtcZIumnD0a2PE7Z2aqjPhSnW1-u-xP3zGA70DtLUU=B3ACBDBB";
        private static readonly string JIRA_ORGANIZATION_ID = "33c618a7-3j7j-178a-6609-39619k487bk9";

        // To test: curl -X GET http://localhost:7235/api/TestJira
        // To test: curl -X GET https://test-jira-sandbox.azurewebsites.net/api/TestJira
        [Function(nameof(TestJira))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest? req = null)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Read the JSON file
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "data.json");
            string jsonData;

            try
            {
                jsonData = await File.ReadAllTextAsync(jsonFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reading JSON file: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            dynamic data = JsonConvert.DeserializeObject(jsonData) ?? new { project_key = "", summary = "", description = "", issue_type = "", };

            if (data.project.id != "" || data.summary != "" || data.description != "" || data.issuetype.id != "")
            {
                // Prepare the Jira API request
                string jiraApiUrl = $"{JIRA_URL}/rest/api/3/issue";
                var client = new HttpClient();
                var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{JIRA_USER}:{JIRA_API_TOKEN}"));

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

                // Create ADF for the description
                var adfDescription = new
                {
                    version = 1,
                    type = "doc",
                    content = new[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = (string)data.description
                                }
                            }
                        }
                    }
                };

                // Customize the payload based on your JSON data
                var payload = new
                {
                    fields = new
                    {
                        project = new { id = (string)data.project.id },
                        summary = (string)data.summary,
                        description = adfDescription,
                        issuetype = new { id = (string)data.issuetype.id }
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                // Make the request to Jira API
                HttpResponseMessage response = null;
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
                    }
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                return new BadRequestObjectResult($"Failed to create Jira ticket, json data is bad");
            }
        }
    }
}
