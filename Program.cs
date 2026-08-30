namespace ScreenTimeTracker;

class Program
{
    static void Main()
    {
        var logic = new Logic();

        DataBase.CreateDB();
        DataBase.LoadDistApps();

        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s,e)=>logic.GetActiveApp();
        timer.Start();

        while (true)
        {
            string[] cmd = Console.ReadLine().Split(" ");
            switch (cmd[0])
            {
                case "screen-time":
                    logic.ShowScreenTime();
                    break;
                case "limits":
                    logic.Limits(cmd);
                    break;
                case "focus-mode":
                    logic.FocusMode(cmd);
                    break;
                case "distracting-apps":
                    logic.DistApps(cmd);
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