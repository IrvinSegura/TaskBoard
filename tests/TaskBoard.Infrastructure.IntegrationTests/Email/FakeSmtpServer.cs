using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TaskBoard.Infrastructure.IntegrationTests.Email;

/// <summary>
/// Just enough of an SMTP server on a local port to see what the real client sends. Nothing leaves this machine.
/// </summary>
internal sealed class FakeSmtpServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _serving;

    public FakeSmtpServer()
    {
        _listener.Start();
        _serving = ServeAsync(_stop.Token);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public List<string> Commands { get; } = [];

    public string? Data { get; private set; }

    private async Task ServeAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                await HandleAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed.
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

        await writer.WriteLineAsync("220 fake ESMTP");

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            Commands.Add(line);

            if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 go ahead");
                Data = await ReadDataAsync(reader, cancellationToken);
                await writer.WriteLineAsync("250 accepted");
            }
            else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 bye");
                return;
            }
            else if (line.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("235 ok");
            }
            else if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250-fake");
                await writer.WriteLineAsync("250 AUTH PLAIN");
            }
            else
            {
                await writer.WriteLineAsync("250 ok");
            }
        }
    }

    private static async Task<string> ReadDataAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var data = new StringBuilder();
        while (await reader.ReadLineAsync(cancellationToken) is { } line && line != ".")
        {
            data.AppendLine(line);
        }

        return data.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        await _serving;
        _stop.Dispose();
    }
}
