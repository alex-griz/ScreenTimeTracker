using Microsoft.Data.Sqlite;
class DataBase
{
    public static string connectionString = "";
    private static string dbDir = "";
    public static string daDir = "";
    public static string dfDir = "";
    public static List<string> DistAppsList = new List<string>();
    public static Dictionary<string, TimeSpan> TimeLimitsList = new Dictionary<string, TimeSpan>();

    public static void Init(string baseDir)
    {
        dbDir = Path.Combine(baseDir, "Database");
        daDir = Path.Combine(dbDir, "DistractingApps.txt");
        dfDir = Path.Combine(dbDir, "STTData.db");
        connectionString = $"Data Source = {dfDir}";
    }
    public static void CreateDB() //единоразовый метод для создания файлов БД
    {
        if (!Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }
        if (!File.Exists(daDir))
        {
            File.Create(daDir).Close();
        }
        if (!File.Exists(dfDir))
        {
            File.Create(dfDir).Close();

            string createServicesTable = "CREATE TABLE TimeData (AppName VARCHAR(64) PRIMARY KEY, Time VARCHAR(24))";
            string createLimitsTable = "CREATE TABLE LimitsData (AppName VARCHAR(64) PRIMARY KEY, TimeLimit VARCHAR(24))";
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = new SqliteCommand(createServicesTable, connection);
            command.ExecuteNonQuery();

            command.CommandText = createLimitsTable;
            command.ExecuteNonQuery();
        }
    }
    public static void LoadDistApps()
    {
        DistAppsList = File.ReadLines(daDir).ToList();
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = new SqliteCommand("SELECT * FROM LimitsData", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            TimeLimitsList[reader["AppName"].ToString()] = TimeSpan.Parse(reader["TimeLimit"].ToString());
        }
    }
}