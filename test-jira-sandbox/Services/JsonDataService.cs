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
                customField1 = "Custom value 1",
                customField2 = "Custom value 2"
            };

            return JsonConvert.SerializeObject(jsonData);
        }
    }
}

