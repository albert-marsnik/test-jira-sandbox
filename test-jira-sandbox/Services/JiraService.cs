using System.Text;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using test_jira_sandbox.Models;

namespace test_jira_sandbox.Services
{
    public class JiraService : IJiraService
    {
        private readonly HttpClient _client;
        private static readonly string? JIRA_API_TOKEN = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
        private static readonly string? JIRA_URL = Environment.GetEnvironmentVariable("JIRA_URL");
        private static readonly string? JIRA_USER = Environment.GetEnvironmentVariable("JIRA_USER");
        private static List<JiraField>? ExistingCustomFields;

        public JiraService(HttpClient client)
        {
            _client = client;
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{JIRA_USER}:{JIRA_API_TOKEN}"));
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task EnsureCustomFieldsExist(IDictionary<string, JToken>? customFields)
        {
            if (customFields == null) return;

            ExistingCustomFields = await GetExistingFields();

            foreach (var customField in customFields)
            {
                if (ExistingCustomFields == null || !ExistingCustomFields.Any(f => f.Name == customField.Key))
                {
                    var newField = new
                    {
                        name = customField.Key,
                        type = "com.atlassian.jira.plugin.system.customfieldtypes:readonlyfield"
                    };

                    var serializedNewField = JsonConvert.SerializeObject(newField);
                    var content = new StringContent(serializedNewField, Encoding.UTF8, "application/json");
                    var response = await _client.PostAsync($"{JIRA_URL}/rest/api/3/field", content);
                    await CheckForError(response);

                }
            }
        }

        public async Task<string> CreateJiraTicket(JiraPayload jiraPayload, JsonSerializerSettings jsonSerializerSettings)
        {
            var jiraApiUrl = $"{JIRA_URL}/rest/api/3/issue";
            var serializedJiraPayload = jiraPayload.SerializeWithoutStatus(jsonSerializerSettings);
            var content = new StringContent(serializedJiraPayload, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(jiraApiUrl, content);
            await CheckForError(response);

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<List<JiraField>> GetExistingFields()
        {
            var response = await _client.GetAsync($"{JIRA_URL}/rest/api/3/field");
            await CheckForError(response);
            var existingFieldsContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<JiraField>>(existingFieldsContent);
        }

        private static async Task CheckForError(HttpResponseMessage? response)
        {
            if (response == null || response.Content == null)
            {
                throw new Exception("response or response.Content is null");
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response != null & response!.Content != null)
                {
                    throw new Exception(await response.Content!.ReadAsStringAsync());
                }
            }

            response.EnsureSuccessStatusCode();
        }
    }
}
