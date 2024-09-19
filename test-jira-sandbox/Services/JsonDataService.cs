using Newtonsoft.Json;
using test_jira_sandbox.Models;

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
                issuetype = new { id = "10001" }
            };

            return JsonConvert.SerializeObject(jsonData);
        }
    }
}
