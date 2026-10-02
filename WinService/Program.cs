using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScreenTimeTracker;

class Program
{
    static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService(o => {o.ServiceName = "ScreenTimeTracker";});
        builder.Logging.AddEventLog(settings=>{settings.SourceName = "ScreenTimeTracker";});

        DataBase.Init(AppContext.BaseDirectory);
        DataBase.CreateDB();
        DataBase.LoadDistApps();

        builder.Services.AddSingleton<Logic>();
        builder.Services.AddHostedService<AppTracker>();
        
        var host = builder.Build();
        host.Run();
    }
}