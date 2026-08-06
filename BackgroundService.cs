using System.ServiceProcess;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace ScreenTimeTracker;
 class BackgroundService : ServiceBase
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private string currentApp = "";
    private  DateTime startTime = DateTime.Now;
    private volatile bool isRunning = false;
    private Thread? workerThread;

    public BackgroundService()
    {
        ServiceName = "STT_BackgroundService";
        CanStop = true;
        CanPauseAndContinue = false;
        AutoLog = true;
    }

    protected override void OnStart(string[] args)
    {
        isRunning = true;
        DataBase.CreateDB();

        workerThread = new Thread(DoWork);
        workerThread.Start();
    }

    private void DoWork()
    {
        using var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s,e)=>GetActiveApp();
        timer.Start();
        while (isRunning)
        {
            Thread.Sleep(5000);
        }
    }
    protected override void OnStop()
    {
        isRunning = false;
        workerThread?.Join(30000);
        TimeSpan workTime = DateTime.Now - startTime;
        Commands.WriteTime(workTime, currentApp);        
    }
    protected override void OnShutdown()
    {
        isRunning = false;
        workerThread?.Join(5000);
        TimeSpan workTime = DateTime.Now - startTime;
        Commands.WriteTime(workTime, currentApp);
    }
    private void GetActiveApp()
    {
        IntPtr hwnd =  GetForegroundWindow();
        if (hwnd == IntPtr.Zero){return;}

        GetWindowThreadProcessId(hwnd, out uint pid);
        Process process = Process.GetProcessById((int)pid);
        if (currentApp != process.ProcessName)
        {
            TimeSpan workTime = DateTime.Now - startTime;
            Commands.WriteTime(workTime, currentApp);

            currentApp = process.ProcessName;
            startTime = DateTime.Now;
        }
    }
}