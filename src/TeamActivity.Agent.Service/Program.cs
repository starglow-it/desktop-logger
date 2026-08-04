using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "TeamActivity Agent Service");
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));
builder.Services.AddHttpClient("manager", client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHostedService<HeartbeatWorker>();

await builder.Build().RunAsync();
