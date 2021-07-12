using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public sealed class HttpClientFactory
{
    // The number of seconds a TCP connection will remain alive/idle before keepalive
    // probes are sent to the remote.
    public static int KEEP_ALIVE_TIME = 3 * 60; // 3 minutes

    // The number of seconds a TCP connection will wait for a keepalive response before
    // sending another keepalive probe.
    public static int KEEP_ALIVE_INTERVAL = 3 * 60; // 3 minutes

    // The number of TCP keep alive probes that will be sent before the connection is
    // terminated.
    public static int KEEP_ALIVE_RETRIES = 16;

    /// <summary>
    ///
    /// </summary>
    /// <param name="verifySSL">is false, SSL-verification is disabled</param>
    /// <param name="timeout">timeout in seconds</param>
    /// <returns>a new `HttpClient` instance</returns>
    public static HttpClient CreateHttpClient(bool verifySSL, int timeout) {
        if( verifySSL ) {
            var handler = new SocketsHttpHandler()
            {
                // uncomment this to enable keep-alive after moving to netcore5
                // ConnectCallback = s_defaultConnectCallback,
            };
            HttpClient httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            return httpClient;
        } else {
            // If we don't want to verify SSL certificate (from the Server), we need to
            // specifically attach a `HttpClientHandler` to `HttpClient` for accepting
            // any certificate from the server. This is useful for testing purposes, but
            // should not be used in production.
            var sslOptions = new SslClientAuthenticationOptions
            {
                // Leave certs unvalidated for debugging
                RemoteCertificateValidationCallback = delegate { return true; },
            };
            var handler = new SocketsHttpHandler()
            {
                // uncomment this to enable keep-alive after moving to netcore5
                // ConnectCallback = s_defaultConnectCallback,
                SslOptions = sslOptions
            };
            HttpClient httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            return httpClient;
        }
    }

    // see the tcp_keepalive struct at the following page for documentation of the buffer layout
    // https://msdn.microsoft.com/en-us/library/windows/desktop/dd877220(v=vs.85).aspx
    // note: a u_long in C is 4 bytes so the C# equivalent is uint

    internal struct KeepAliveValues
    {
        public uint OnOff { get; set; }
        public uint KeepAliveTime { get; set; }
        public uint KeepAliveInterval { get; set; }

        public byte[] ToBytes()
        {
            var bytes = new byte[12];
            Array.Copy(BitConverter.GetBytes(OnOff), 0, bytes, 0, 4);
            Array.Copy(BitConverter.GetBytes(KeepAliveTime), 0, bytes, 4, 4);
            Array.Copy(BitConverter.GetBytes(KeepAliveInterval), 0, bytes, 8, 4);
            return bytes;
        }
    }

    private static Socket CreateSocket(EndPoint endPoint)
    {
        var addressFamily = endPoint.AddressFamily;
        Socket socket = null;
        if (addressFamily == AddressFamily.Unspecified || addressFamily == AddressFamily.Unknown)
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        } else {
            socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        // not all platforms support IOControl
        try
        {
            var keepAliveValues = new KeepAliveValues
            {
                OnOff = 1,
                KeepAliveTime = (uint) KEEP_ALIVE_TIME * 1000, // KEEP_ALIVE_TIME seconds in milliseconds
                KeepAliveInterval = (uint) KEEP_ALIVE_INTERVAL * 1000 // KEEP_ALIVE_INTERVAL seconds in milliseconds
            };
            socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues.ToBytes(), null);
        }
        catch (PlatformNotSupportedException)
        {
            // most platforms should support this call to SetSocketOption, but just in case call it in a try/catch also
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, KEEP_ALIVE_TIME);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, KEEP_ALIVE_INTERVAL);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, KEEP_ALIVE_RETRIES);
            }
            catch (PlatformNotSupportedException)
            {
                // ignore PlatformNotSupportedException
            }
        }


        return socket;
    }

    /*
    // uncomment this block to enable keep-alive after moving to netcore5
    private static async ValueTask<Stream> DefaultConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        // `Socket.SetSocketOption` function fails if a `DnsEndPoint` is passed, as it
        // cannot set the option for multiple target URLs and keep track of all of them
        // That's why we need to get the IP address here first.
        // Note: this is necessary for the keep-alive option
        // Risk: if the DNS data changes, we'll be in trouble. Then, we should not reuse
        // this socket (or we'll risk using a possibly out-dated DNS entry)
        IPEndPoint ipEndPoint = await getIPEndPoint(context.DnsEndPoint).ConfigureAwait(false);
        Socket socket = CreateSocket(ipEndPoint);
        socket.NoDelay = true;
        try
        {
            await socket.ConnectAsync(ipEndPoint, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
    private static readonly Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> s_defaultConnectCallback = DefaultConnectAsync;
    */

    private async static ValueTask<IPEndPoint> getIPEndPoint(DnsEndPoint dnsEndPoint)
    {
        var uriBuilder = new UriBuilder();
        uriBuilder.Host = dnsEndPoint.Host;
        uriBuilder.Port = dnsEndPoint.Port;
        // Get DNS host information.
        IPAddress[] hostAdresses = await Dns.GetHostAddressesAsync(uriBuilder.Uri.DnsSafeHost).ConfigureAwait(false);
        // try IPv4 and IPv6 address
        IPAddress addressV4 = null;
        IPAddress addressV6 = null;
        foreach (IPAddress address in hostAdresses)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (addressV6 == null)
                {
                    addressV6 = address;
                }
            }
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                if (addressV4 == null)
                {
                    addressV4 = address;
                }
            }
            if ((addressV4 != null) && (addressV6 != null))
            {
                break;
            }
        }
        IPAddress ipAddress = addressV4 == null ? addressV6 : addressV4;
        return new IPEndPoint(ipAddress, dnsEndPoint.Port);
    }
}
