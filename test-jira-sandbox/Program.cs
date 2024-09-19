using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using test_jira_sandbox.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddSingleton<IJsonDataService, JsonDataService>();
        services.AddHttpClient<IJiraService, JiraService>();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
