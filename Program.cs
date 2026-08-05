using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace ScreenTimeTracker;

class Program
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private static string currentApp = "";
    private static DateTime startTime = DateTime.Now;
    static void Main()
    {
        DataBase.CreateDB();
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s,e)=>GetActiveApp();
        timer.Start();

        Console.WriteLine("Launched");
        Console.ReadKey();
    }
    private static void GetActiveApp()
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
            Console.WriteLine($"[DEBUG]Active window changed. New window:{currentApp}");
        }
    }
}
