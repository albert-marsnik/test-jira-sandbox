using Newtonsoft.Json;

namespace test_jira_sandbox.Services
{
    public class JsonDataService : IJsonDataService
    {
        public string GetJsonData()
        {
            var jsonData = new
            {
                project = new { id = "10000" },
                summary = "Issue summary",
                description = "Detailed description of the issue",
                issuetype = new { id = "10001" },
                assignee = new { id = "712020:1ae9fed4-a563-46d6-8119-7dc26e704c6c" },
                status = new { id = "10003" },
                labels = new List<string>() { "label-1", "label-2", "label-3" },
                customfield_10037 = "Custom value 1",
                customfield_10038 = "Custom value 2",
                duedate = "2029-01-01",
                //fixVersions = "2025.1.0"
            };

            return JsonConvert.SerializeObject(jsonData);
        }
    }
}

