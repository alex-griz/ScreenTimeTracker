using System.ServiceProcess;

namespace ScreenTimeTracker;

class Program
{
    
    static void Main()
    {
        ServiceBase.Run(new BackgroundService());
    }
}
