using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
class DataBase
{
    public static readonly string connectionString = $"Data Source= Database/STTData.db";
    public static List<string> DistAppsList = new List<string>();
    public static Dictionary<string, TimeSpan> TimeLimitsList = new Dictionary<string, TimeSpan>();

    public static void CreateDB() //единоразовый метод для создания файлов БД
    {
        if (!Directory.Exists("Database"))
        {
            Directory.CreateDirectory("Database");
        }
        if (!File.Exists("Database/DistractingApps.txt"))
        {
            File.Create("Database/DistractingApps.txt").Close();
        }
        if (!File.Exists("Database/STTData.db"))
        {
            File.Create("Database/STTData.db").Close();

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
        DistAppsList = File.ReadLines("Database/DistractingApps.txt").ToList();
        
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