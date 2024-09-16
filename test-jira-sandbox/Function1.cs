using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;

public static class TestJira

{
    private static readonly string JIRA_URL = "https://ti8m-marsnik.atlassian.net";
    private static readonly string JIRA_USER = "albert.marsnik@ti8m.ch";
    private static readonly string JIRA_API_TOKEN = "ATCTT3xFfGN0jzb_aPYeBn40QAtt8QFzFhoKfFHngoNIh-avmJHcXz74DzgO1Y5kCGBCp4yglR1klnXdEC2_iOCDrSWtdcOtCSQKaEgWDVZoGFkUd4pgQzEp78JPhgZFCjfyM-awVTmfoMuNf3CzQIycyZ8TtZ2ce0pIq91xS0vGiY3CkpHBIMk=717E28A3";
    private static readonly string JIRA_ORGANIZATION_ID = "33c618a7-3j7j-178a-6609-39619k487bk9";

    [FunctionName(nameof(TestJira))]

    public static async Task<IActionResult> Run(

        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,

        ILogger log)

    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        // Read the JSON file
        string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "data.json");
        string jsonData;

        try
        {

            jsonData = await File.ReadAllTextAsync(jsonFilePath);
        }
        catch (Exception ex)
        {
            log.LogError($"Error reading JSON file: {ex.Message}");

            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        dynamic data = JsonConvert.DeserializeObject(jsonData) ?? false;

        if (data)
        {
            // Prepare the Jira API request
            string jiraApiUrl = $"{JIRA_URL}/rest/api/2/issue";
            var client = new HttpClient();
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{JIRA_USER}:{JIRA_API_TOKEN}"));

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
            client.DefaultRequestHeaders.Add("Content-Type", "application/json");

            // Customize the payload based on your JSON data
            var payload = new
            {
                fields = new
                {
                    project = new { key = (string)data.project_key },
                    summary = (string)data.summary,
                    description = (string)data.description,
                    issuetype = new { name = (string)data.issue_type }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            // Make the request to Jira API
            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(jiraApiUrl, content);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                log.LogError($"Error creating Jira ticket: {ex.Message}");

                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new OkObjectResult($"Jira ticket created successfully: {await response.Content.ReadAsStringAsync()}");
        }
        else
        {
            return new BadRequestObjectResult($"Failed to create Jira ticket, json data is bad");
        }
    }
}