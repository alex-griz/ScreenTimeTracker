using Microsoft.Data.Sqlite;
class DataBase
{
    public static readonly string connectionString = $"Data Source= Database/STTData.db";

    public static void CreateDB() //единоразовый метод для создания файлов БД
    {
        if (!Directory.Exists("Database"))
        {
            Directory.CreateDirectory("Database");
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
}