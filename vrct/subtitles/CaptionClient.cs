using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VrcEnglishSubtitles
{
    public sealed class ConnectionNotice
    {
        public bool Connected { get; set; }
        public string Message { get; set; }
    }

    public sealed class CaptionClient : IDisposable
    {
        private readonly string configFile;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private ClientWebSocket socket;
        private Task worker;
        public string LastFailureCategory { get; private set; }
        public event Action<Caption> CaptionReceived;
        public event Action<ConnectionNotice> ConnectionChanged;

        public CaptionClient(string file) { configFile = file; }

        public void Start()
        {
            if (worker != null) return;
            worker = Task.Run((Func<Task>)RunAsync);
        }

        private void Notice(bool connected, string message)
        {
            var handler = ConnectionChanged;
            if (handler != null) handler(new ConnectionNotice { Connected = connected, Message = message });
        }

        private async Task RunAsync()
        {
            while (!shutdown.IsCancellationRequested)
            {
                try
                {
                    var settings = ConnectionSettings.Load(configFile);
                    if (!settings.Enabled)
                    {
                        Notice(false, "首次使用：退出 VRCT，再从桌面入口重新打开");
                        await Task.Delay(2000, shutdown.Token).ConfigureAwait(false);
                        continue;
                    }
                    Notice(false, "正在连接翻译器…");
                    using (var connection = new ClientWebSocket())
                    {
                        socket = connection;
                        connection.Options.Proxy = null;
                        connection.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token))
                        {
                            timeout.CancelAfter(6000);
                            await connection.ConnectAsync(settings.Address, timeout.Token).ConfigureAwait(false);
                        }
                        Notice(true, "已连接 · 等待对方说英语");
                        var buffer = new byte[8192];
                        while (connection.State == WebSocketState.Open && !shutdown.IsCancellationRequested)
                        {
                            using (var message = new MemoryStream())
                            {
                                WebSocketReceiveResult result;
                                do
                                {
                                    result = await connection.ReceiveAsync(new ArraySegment<byte>(buffer), shutdown.Token).ConfigureAwait(false);
                                    if (result.MessageType == WebSocketMessageType.Close) break;
                                    message.Write(buffer, 0, result.Count);
                                    if (message.Length > 1048576) throw new InvalidDataException("Subtitle message exceeds limit");
                                } while (!result.EndOfMessage);
                                if (result.MessageType == WebSocketMessageType.Close) break;
                                if (result.MessageType != WebSocketMessageType.Text) continue;
                                Caption caption;
                                if (CaptionParser.TryParse(Encoding.UTF8.GetString(message.ToArray()), out caption))
                                {
                                    var handler = CaptionReceived;
                                    if (handler != null) handler(caption);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { if (shutdown.IsCancellationRequested) break; }
                catch (Exception error)
                {
                    // Do not expose a connection URL or authentication token in UI/logs.
                    LastFailureCategory = error.GetType().Name;
                }
                finally { socket = null; }
                if (shutdown.IsCancellationRequested) break;
                Notice(false, "等待翻译器连接 · 将自动重试");
                try { await Task.Delay(1500, shutdown.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        public void Dispose()
        {
            shutdown.Cancel();
            var active = socket;
            if (active != null) { try { active.Abort(); } catch (ObjectDisposedException) { } }
        }
    }
}
