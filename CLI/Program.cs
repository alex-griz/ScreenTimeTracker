using System.IO.Pipes;
using System.Text;
using System.Text.Json;
namespace SttCLI;
class Program
{
    private static readonly string pipeName = "stt-pipe";
    static async Task<int> Main(string[] args)
    {
        if(args.Length == 0)
        {
            Console.WriteLine("Using command: stt <command>");
            return 1;
        }

        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            await client.ConnectAsync(3000);

            using var reader = new StreamReader(client, Encoding.UTF8);
            using var writer = new StreamWriter(client, Encoding.UTF8){AutoFlush=true};

            await writer.WriteLineAsync(JsonSerializer.Serialize(args));

            var response = await reader.ReadLineAsync();
            var message = JsonSerializer.Deserialize<string>(response) ?? "";
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("Service is not avaible");
                return 1;
            }
            Console.WriteLine(message);
            return 0;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
