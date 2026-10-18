using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;

namespace ScreenTimeTracker;

public class PipeServer : BackgroundService
{
    public const string pipeName = "stt-pipe";

    private readonly Logic _logic;
    private readonly ILogger<PipeServer> _logger;

    public PipeServer(Logic logic, ILogger<PipeServer> logger)
    {
        _logic = logic;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Pipe server started on {Pipe}", pipeName);

        while (!ct.IsCancellationRequested)
        {
            var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            try
            {
                await server.WaitForConnectionAsync(ct);
                _ = Task.Run(() => HandleClientAsync(server,ct), ct);
            }
            catch (OperationCanceledException)
            {
                server.Dispose();
                break;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                server.Dispose();
            }
        }
    }
    private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken ct)
    {
        using (server)
        using (var reader = new StreamReader(server, Encoding.UTF8))
        using (var writer = new StreamWriter(server, Encoding.UTF8) { AutoFlush = true })
        {
            try
            {
                var json = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(json)) return;

                var cmd = JsonSerializer.Deserialize<string[]>(json);
                if (cmd == null || cmd.Length == 0) return;

                string output;
                try
                {
                    output = _logic.Execute(cmd);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                    output = ex.Message;
                }
                var response = JsonSerializer.Serialize(output);
                await writer.WriteLineAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}