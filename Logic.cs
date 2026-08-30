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

    private string currentApp = "";
    private List<string> blockedApps = new List<string>();

    private DateTime startTime = DateTime.Now;
    public bool isFocusModeEnabled = false;

    private System.Timers.Timer? appTimer = null;
    private System.Timers.Timer? focusTimer = null;

    private void WriteTime(TimeSpan workTime, string name)
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
    public void GetActiveApp()
    {
        IntPtr hwnd =  GetForegroundWindow();
        if (hwnd == IntPtr.Zero){return;}

        GetWindowThreadProcessId(hwnd, out uint pid);
        Process process = Process.GetProcessById((int)pid);
        if ((DataBase.DistAppsList.Contains(process.ProcessName) && isFocusModeEnabled) || blockedApps.Contains(process.ProcessName))
        {
            process.Kill();
            Console.WriteLine($"App {process.ProcessName} is blocked");
            return;
        }
        if (currentApp != process.ProcessName)
        {
            TimeSpan workTime = DateTime.Now - startTime;
            if(appTimer != null){appTimer.Stop(); appTimer.Dispose(); appTimer = null; DataBase.TimeLimitsList[currentApp] -= workTime;}
            WriteTime(workTime, currentApp);

            currentApp = process.ProcessName;
            startTime = DateTime.Now;
        }
        if (DataBase.TimeLimitsList.ContainsKey(currentApp) && appTimer == null)
        {
            appTimer = new System.Timers.Timer(DataBase.TimeLimitsList[currentApp].TotalMilliseconds);
            appTimer.AutoReset = false;
            appTimer.Elapsed += (s,e) => 
            {
                string targetApp = currentApp;
                try
                {
                    var processes = Process.GetProcessesByName(targetApp);
                    foreach (var p in processes)
                    {
                        p.Kill();
                    }
                    blockedApps.Add(targetApp);
                }
                catch{}
                finally
                {
                    appTimer?.Dispose();
                    appTimer = null;
                }
                
                Console.WriteLine($"Time limit for {currentApp} is over today");
                return;
            };
            appTimer.Start();
        }
    }
    public void ShowScreenTime()
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
    public void DistApps(string[] cmd)
    {
        if (cmd.Length < 2)
        {
            Console.WriteLine("List of distracting apps:");
            foreach (string i in DataBase.DistAppsList)
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
                    DataBase.DistAppsList.Add(cmd[2]);
                    break;
                case "remove":
                    File.WriteAllText("Database/DistractingApps.txt", string.Empty);
                    DataBase.DistAppsList.Remove(cmd[2]);
                    foreach(string i in DataBase.DistAppsList)
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
    public void Limits(string[] cmd)
    {
        if (cmd.Length < 2)
        {
            foreach(KeyValuePair<string,TimeSpan> pair in DataBase.TimeLimitsList)
            {
                Console.WriteLine($"App:   {pair.Key}     Time Limit:   {pair.Value}");
            }
        }
        else
        {
            if(cmd.Length < 4 && cmd[2] != "remove"){Console.WriteLine("Using this command: limits add/remove/edit <appname> <hh:mm:ss"); return;}
            using var  connection = new SqliteConnection(DataBase.connectionString);
            connection.Open();
            using var command = new SqliteCommand("", connection);
            switch (cmd[1])
            {
                case "add":
                    command.CommandText = "INSERT INTO LimitsData (AppName , TimeLimit) VALUES (@N, @T)";
                    command.Parameters.AddWithValue("@T", cmd[3]);
                    DataBase.TimeLimitsList[cmd[2]] = TimeSpan.Parse(cmd[3]);
                    break;
                case "remove":
                    command.CommandText = "DELETE FROM LimitsData WHERE Appname = @N";
                    DataBase.TimeLimitsList.Remove(cmd[2]);
                    break;
                case "edit":
                    command.CommandText = "UPDATE LimitsData SET TimeLimit = @T WHERE AppName = @N";
                    command.Parameters.AddWithValue("@T", cmd[3]);
                    DataBase.TimeLimitsList[cmd[2]] = TimeSpan.Parse(cmd[3]);
                    break;
                default:
                    Console.WriteLine($"Unknown argument {cmd[1]}");
                    return;
            }
            command.Parameters.AddWithValue("@N", cmd[2]);
            command.ExecuteNonQuery();
        }
    }
    public void FocusMode(string[] cmd)
    {
        if (cmd.Length < 2)
        {
            Console.WriteLine("Using this command: focus-mode enable/disable hh:mm:ss(optional, only if enable)");
            return;
        }
        switch (cmd[1])
        {
            case "enable":
                if (focusTimer != null)
                {
                    focusTimer.Stop();
                    focusTimer.Dispose();
                    focusTimer = null;
                }
                if (cmd.Length > 2)
                {
                    try
                    {
                        double time = TimeSpan.Parse(cmd[2]).TotalMilliseconds;

                        focusTimer = new System.Timers.Timer(time);
                        focusTimer.AutoReset = false;
                        focusTimer.Elapsed += (s,e) => 
                        {
                            isFocusModeEnabled = false; 
                            focusTimer?.Dispose();
                            focusTimer = null;
                            Console.WriteLine("Focus disabled"); 
                        };
                        isFocusModeEnabled = true;
                        Console.WriteLine("Focus enabled");
                        focusTimer.Start();
                        return;
                    }
                    catch
                    {
                        Console.WriteLine("Using this command: focus-mode enable/disable hh:mm:ss(optional, only if enable)");
                        return;
                    }
                }

                isFocusModeEnabled = true;
                Console.WriteLine("Focus enabled");
                break;
            case "disable":
                if(focusTimer != null)
                {
                    focusTimer.Stop();
                    focusTimer.Dispose();
                    focusTimer = null;
                }
                isFocusModeEnabled = false;
                Console.WriteLine("Focus disabled");
                break;
            default:
                Console.WriteLine($"Unknown argument {cmd[1]}");
                break;
        }
    }
}