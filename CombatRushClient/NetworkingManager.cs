using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CombatRushClient;

public class NetworkingManager
{
    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;
    private Task _recvTask;
    private ConcurrentQueue<byte[]> _inbound;

    public NetworkingManager(ConcurrentQueue<byte[]> inbound)
    {
        _inbound = inbound;
    }

    public async void Initialize(string host, int port, CancellationToken ct = default)
    {
        // send join request
        // Task.Run(async _ => { }).Result;

        // get affirmation

        // start receiving thread

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, ct).ConfigureAwait(false);
        _tcpClient.NoDelay = true;
        _stream = _tcpClient.GetStream();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _recvTask = Task.Run(() => ReceiveLoopAsync(_cts.Token), ct);
    }

    public async Task SendAsync(byte[] payload, CancellationToken ct = default)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected.");

        await _stream.WriteAsync(payload, ct).ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Read 4-byte big-endian length
                var lenBuf = await ReadExactAsync(_stream, 4, ct).ConfigureAwait(false);
                var len = BinaryPrimitives.ReadUInt32BigEndian(lenBuf);

                if (len == 0) continue; // ignore empty frames

                // Read payload
                var payload = await ReadExactAsync(_stream, checked((int)len), ct).ConfigureAwait(false);
                
                _inbound.Enqueue(payload);
            }
        }
        catch (OperationCanceledException)
        {
            /* normal shutdown */
        }
        catch (IOException)
        {
            /* disconnected */
        }
        catch (ObjectDisposedException)
        {
            /* shutting down */
        }
    }

    private static async Task<byte[]> ReadExactAsync(Stream s, int length, CancellationToken ct)
    {
        var buf = new byte[length];
        var read = 0;
        while (read < length)
        {
            var r = await s.ReadAsync(buf.AsMemory(read, length - read), ct).ConfigureAwait(false);
            if (r == 0) throw new IOException("Remote closed.");
            read += r;
        }

        return buf;
    }

    public async ValueTask DisposeAsync()
    {
        // Graceful shutdown
        if (_cts is not null && !_cts.IsCancellationRequested)
            _cts.Cancel();

        if (_recvTask is not null)
        {
            try
            {
                await _recvTask.ConfigureAwait(false);
            }
            catch
            {
                /* ignore */
            }
        }

        _stream?.Dispose();
        _tcpClient?.Close();
        _cts?.Dispose();
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}