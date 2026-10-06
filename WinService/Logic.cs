using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenTimeTracker;
class Logic
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
        
    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    private string currentApp = "";
    private List<string> blockedApps = new List<string>();

    private DateTime startTime = DateTime.Now;
    public bool isFocusModeEnabled = false;

    private System.Timers.Timer? appTimer = null;
    private System.Timers.Timer? focusTimer = null;
    public string Execute(string[] cmd)
    {
        switch (cmd[0])
        {
            case "screen-time": return ShowScreenTime();
            case "limits": return Limits(cmd);
            case "focus-mode": return FocusMode(cmd);
            case "find-apps": return FindApps();
            case "distracting-apps": return DistApps(cmd);
            default: return $"Unknown command {cmd[0]}";
        }
    }

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
            if(process.CloseMainWindow() && process.WaitForExit(3000))
            {
                //Console.WriteLine($"App {process.ProcessName} is blocked");
            }
            else
            {
                process.Kill();
            }
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
                    var process = Process.GetProcessesByName(targetApp).First();
                    if(process.CloseMainWindow() && process.WaitForExit(3000))
                    {
                        //Console.WriteLine($"Time limit for {currentApp} is over today");
                    }
                    else
                    {
                        process.Kill();
                    }
                    blockedApps.Add(targetApp);
                    return;
                }
                catch{}
                finally
                {
                    appTimer?.Dispose();
                    appTimer = null;
                }
            };
            appTimer.Start();
        }
    }
    public string ShowScreenTime()
    {
        var answer = new StringBuilder();
        using var connection = new SqliteConnection(DataBase.connectionString);
        connection.Open();
        using var command = new SqliteCommand("SELECT * FROM TimeData", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            answer.AppendLine($"{reader["AppName"]} :      {reader["Time"]}");
        }
        return answer.ToString();
    }
    public string DistApps(string[] cmd)
    {
        var answer = new StringBuilder();
        if (cmd.Length < 2)
        {
            answer.AppendLine("List of distracting apps:");
            foreach (string i in DataBase.DistAppsList)
            {
                answer.AppendLine(i);
            }
            return answer.ToString();
        }
        else
        {
            if (cmd.Length < 3){answer.AppendLine("Using this command: distracting-apps add/remove <appname>"); return answer.ToString();}
            using StreamWriter writer = new StreamWriter("Database/DistractingApps.txt");
            switch (cmd[1])
            {
                case "add":
                    writer.WriteLine(cmd[2]);
                    DataBase.DistAppsList.Add(cmd[2]);
                    return "Success";
                case "remove":
                    File.WriteAllText("Database/DistractingApps.txt", string.Empty);
                    DataBase.DistAppsList.Remove(cmd[2]);
                    foreach(string i in DataBase.DistAppsList)
                    {
                        writer.WriteLine(i);
                    }
                    return "Success";
                default:
                    answer.AppendLine($"Unknown argument {cmd[1]}");
                    return answer.ToString();
            }
        }
    }
    public string Limits(string[] cmd)
    {
        var answer = new StringBuilder();
        if (cmd.Length < 3)
        {
            foreach(KeyValuePair<string,TimeSpan> pair in DataBase.TimeLimitsList)
            {
                answer.AppendLine($"App:   {pair.Key}     Time Limit:   {pair.Value}");
            }
            return answer.ToString();
        }
        else
        {
            if(cmd.Length < 4 && cmd[2] != "remove"){answer.AppendLine("Using this command: limits add/remove/edit <appname> <hh:mm:ss"); return answer.ToString();}
            using var  connection = new SqliteConnection(DataBase.connectionString);
            connection.Open();
            using var command = new SqliteCommand("", connection);
            switch (cmd[1])
            {
                case "add":
                    command.CommandText = "INSERT INTO LimitsData (AppName , TimeLimit) VALUES (@N, @T)";
                    command.Parameters.AddWithValue("@T", cmd[3]);
                    DataBase.TimeLimitsList[cmd[2]] = TimeSpan.Parse(cmd[3]);
                    answer.AppendLine("Success");
                    break;
                case "remove":
                    command.CommandText = "DELETE FROM LimitsData WHERE Appname = @N";
                    DataBase.TimeLimitsList.Remove(cmd[2]);
                    answer.AppendLine("Success");
                    break;
                case "edit":
                    command.CommandText = "UPDATE LimitsData SET TimeLimit = @T WHERE AppName = @N";
                    command.Parameters.AddWithValue("@T", cmd[3]);
                    DataBase.TimeLimitsList[cmd[2]] = TimeSpan.Parse(cmd[3]);
                    answer.AppendLine("Success");
                    break;
                default:
                    answer.AppendLine($"Unknown argument {cmd[1]}");
                    break;
            }
            command.Parameters.AddWithValue("@N", cmd[2]);
            command.ExecuteNonQuery();
            return answer.ToString();
        }
    }
    public string FocusMode(string[] cmd)
    {
        var answer = new StringBuilder();
        if (cmd.Length < 2)
        {
            answer.AppendLine("Using this command: focus-mode enable/disable hh:mm:ss(optional, only if enable)");
            return answer.ToString();
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
                        };
                        isFocusModeEnabled = true;
                        answer.AppendLine("Focus enabled");
                        focusTimer.Start();
                        return answer.ToString();
                    }
                    catch
                    {
                        answer.AppendLine("Using this command: focus-mode enable/disable hh:mm:ss(optional, only if enable)");
                        return answer.ToString();
                    }
                }

                isFocusModeEnabled = true;
                answer.AppendLine("Focus enabled");
                break;
            case "disable":
                if(focusTimer != null)
                {
                    focusTimer.Stop();
                    focusTimer.Dispose();
                    focusTimer = null;
                }
                isFocusModeEnabled = false;
                answer.AppendLine("Focus disabled");
                break;
            default:
                answer.AppendLine($"Unknown argument {cmd[1]}");
                break;
        }
        return answer.ToString();
    }
    public string FindApps()
    {
        var answer = new StringBuilder();
        var windows = new List<string>();
        IntPtr desktop = GetShellWindow();
        
        EnumWindows((hWnd, lParam) =>
        {
            if (hWnd != desktop && IsWindowVisible(hWnd))
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                try
                {
                    Process process = Process.GetProcessById((int)processId);
                    if (!string.IsNullOrEmpty(process.MainWindowTitle))
                    {
                        windows.Add(process.ProcessName+" => "+process.MainWindowTitle);
                    }
                }
                catch
                {}
            }
            return true;
        }, IntPtr.Zero);
        
        answer.AppendLine("List of active apps: (Process Name > Window Title):");
        foreach(string w in windows)
        {
            answer.AppendLine($"\n   {w}");
        }
        return answer.ToString();
    }
}