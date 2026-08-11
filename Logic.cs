using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
namespace ScreenTimeTracker;
class Logic
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private static string currentApp = "";
    private static DateTime startTime = DateTime.Now;

    private static void WriteTime(TimeSpan workTime, string name)
    {
        TimeSpan totalTime = workTime;
        using var connection = new SqliteConnection(DataBase.connectionString);
        connection.Open();
        using var command = new SqliteCommand("SELECT Time FROM TimeData WHERE AppName = @N", connection);
        command.Parameters.AddWithValue("@N", name);
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            totalTime = totalTime + TimeSpan.Parse(reader["Time"].ToString());
        }
        reader.Close();

        command.CommandText = "INSERT OR REPLACE INTO TimeData (AppName, Time) VALUES (@N, @T)";
        command.Parameters.AddWithValue("@T", totalTime.ToString());
        command.ExecuteNonQuery();
    }
    public static void GetActiveApp()
    {
        IntPtr hwnd =  GetForegroundWindow();
        if (hwnd == IntPtr.Zero){return;}

        GetWindowThreadProcessId(hwnd, out uint pid);
        Process process = Process.GetProcessById((int)pid);
        if (currentApp != process.ProcessName)
        {
            TimeSpan workTime = DateTime.Now - startTime;
            WriteTime(workTime, currentApp);

            currentApp = process.ProcessName;
            startTime = DateTime.Now;
        }
    }
    public static void ShowScreenTime()
    {
        using var connection = new SqliteConnection(DataBase.connectionString);
        connection.Open();
        using var command = new SqliteCommand("SELECT * FROM TimeData", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"{reader["AppName"]} :      {reader["Time"]}");
        }
    }
}