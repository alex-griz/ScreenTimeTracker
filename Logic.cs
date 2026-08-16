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
    private static string[] blockedApps = [];

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
    public static void DistApps(string[] cmd)
    {
        string[] apps = [];
        using StreamReader reader = new StreamReader("Database/DistractingApps.txt");
        string line = reader.ReadLine();

        while (line != null)
        {
            apps.Append(line);
        }
        reader.Close();
        if (cmd.Length < 2)
        {
            Console.WriteLine("List of distracting apps:");
            foreach (string i in apps)
            {
                Console.WriteLine(i);
            }
        }
        else
        {
            if (cmd.Length < 3){Console.WriteLine("Using this command: distracting-apps add/remove <appname>"); return;}
            using StreamWriter writer = new StreamWriter("Database/DistractingApps.txt");
            switch (cmd[1])
            {
                case "add":
                    writer.WriteLine(cmd[2]);
                    break;
                case "remove":
                    File.WriteAllText("Database/DistractingApps.txt", string.Empty);
                    apps = apps.Where(x => x != cmd[2]).ToArray();
                    foreach(string i in apps)
                    {
                        writer.WriteLine(i);
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown argument {cmd[1]}");
                    break;
            }
        }
    }
}