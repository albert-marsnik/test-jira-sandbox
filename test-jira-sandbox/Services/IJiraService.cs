using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using test_jira_sandbox.Models;

namespace test_jira_sandbox.Services
{
    public interface IJiraService
    {
        Task EnsureCustomFieldsExist(IDictionary<string, JToken>? customFields);
        Task<string> CreateJiraTicket(JiraPayload jiraPayload, JsonSerializerSettings jsonSerializerSettings);
    }
}
