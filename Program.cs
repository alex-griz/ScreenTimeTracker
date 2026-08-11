namespace ScreenTimeTracker;

class Program
{
    static void Main()
    {
        DataBase.CreateDB();
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s,e)=>Logic.GetActiveApp();
        timer.Start();
        
        while (true)
        {
            string[] cmd = Console.ReadLine().Split(" ");
            switch (cmd[0])
            {
                case "screen-time":
                    Logic.ShowScreenTime();
                    break;
                case "limits":
                    break;
                case "focus-mode":
                    break;
                case "distracting-apps":
                    break;
                case "help":
                    break;
                default:
                    Console.WriteLine($"Unknown command {cmd[0]}");
                    break;
            }
        }
    }
}