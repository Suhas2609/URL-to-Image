using System.Net;
using System.Net.Sockets;
using VehicleImage.Api.Configuration;
using VehicleImage.Api.Exceptions;

namespace VehicleImage.Api.Security;

public static class SsrfGuardHttpHandler
{
    public const string ClientName = "SsrfSafeRetrievalClient";

    public static SocketsHttpHandler Create(DestinationValidator destinationValidator, RetrievalOptions options)
    {
        ArgumentNullException.ThrowIfNull(destinationValidator);
        ArgumentNullException.ThrowIfNull(options);

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            ConnectTimeout = TimeSpan.FromSeconds(options.ConnectTimeoutSeconds),
            PooledConnectionLifetime = TimeSpan.FromSeconds(options.PooledConnectionLifetimeSeconds),
            MaxResponseHeadersLength = 64,
            ConnectCallback = async (context, cancellationToken) =>
            {
                string host = context.DnsEndPoint.Host;
                int port = context.DnsEndPoint.Port;

                if (!destinationValidator.IsPortAllowed(port, options.AllowedPorts))
                {
                    throw ImageIngestionException.UnsafeDestination($"Port {port} is not permitted.");
                }

                IReadOnlyList<IPAddress> validatedAddresses =
                    await destinationValidator.ResolveAndValidateAsync(host, cancellationToken);

                Exception? lastSocketFailure = null;

                foreach (var address in validatedAddresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = true
                    };

                    try
                    {
                        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        connectCts.CancelAfter(TimeSpan.FromSeconds(options.ConnectTimeoutSeconds));

                        await socket.ConnectAsync(new IPEndPoint(address, port), connectCts.Token);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        socket.Dispose();
                        lastSocketFailure = ImageIngestionException.ConnectionTimeout(inner: ex);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        socket.Dispose();
                        lastSocketFailure = ex;
                    }
                }

                if (lastSocketFailure is ImageIngestionException ingestionEx)
                {
                    throw ingestionEx;
                }

                throw ImageIngestionException.ConnectionTimeout(
                    "Unable to establish a TCP connection to the supplier server.",
                    lastSocketFailure);
            }
        };

        return handler;
    }
}
