using System.Text;
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

        public JiraService(HttpClient client)
        {
            _client = client;
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{JIRA_USER}:{JIRA_API_TOKEN}"));
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task EnsureCustomFieldsExist(IDictionary<string, JToken>? customFields)
        {
            var completeGetExistingFieldsUrl = $"{JIRA_URL}/rest/api/3/field";
            var response = await _client.GetAsync(completeGetExistingFieldsUrl);
            await CheckForError(response, nameof(EnsureCustomFieldsExist), completeGetExistingFieldsUrl);
            var existingFieldsContent = await response.Content.ReadAsStringAsync();

            var existingFields = JsonConvert.DeserializeObject<List<JiraField>>(existingFieldsContent);

            if (customFields == null) return;

            List<KeyValuePair<string, JToken>> missingCustomFields = [];

            foreach (var customField in customFields)
            {
                if (existingFields == null || !existingFields.Any(field => field.Key == customField.Key || field.Id == customField.Key))
                {
                    missingCustomFields.Add(customField);
                }
            }

            if (missingCustomFields.Count > 0)
            {
                throw new Exception($"The following custom fields were defined in the json, but aren't defined in Jira: {string.Join(", ", missingCustomFields.Select(field => $"{field.Key}: {field.Value}"))}");
            }
        }

        public async Task<string> CreateJiraTicket(JiraPayload jiraPayload, JsonSerializerSettings jsonSerializerSettings)
        {
            var completeCreateJiraTicketUrl = $"{JIRA_URL}/rest/api/3/issue";
            var serializedJiraPayload = jiraPayload.SerializeWithoutStatus(jsonSerializerSettings);
            var content = new StringContent(serializedJiraPayload, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(completeCreateJiraTicketUrl, content);
            await CheckForError(response, nameof(CreateJiraTicket), completeCreateJiraTicketUrl);

            return await response.Content.ReadAsStringAsync();
        }

        private static async Task CheckForError(HttpResponseMessage? response, string? apiCallName, string? apiUrl)
        {
            var apiCallNameAndUrl = $"{apiCallName} ({apiUrl}): ";
            if (response == null || response.Content == null)
            {
                throw new Exception($"{apiCallNameAndUrl}response or response.Content is null");
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response != null & response!.Content != null)
                {
                    throw new Exception($"{apiCallNameAndUrl}{await response.Content!.ReadAsStringAsync()}");
                }
            }

            response.EnsureSuccessStatusCode();
        }
    }
}
