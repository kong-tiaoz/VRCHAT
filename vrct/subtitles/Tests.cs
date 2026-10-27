using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace VrcEnglishSubtitles
{
    public static class Tests
    {
        private static int assertions;
        private static void Check(bool condition, string description)
        { assertions++; if (!condition) throw new Exception(description); }

        private static string Payload(string type, string message, string translation)
        { return JsonFiles.Serializer().Serialize(new { type = type, message = message, translation = new[] { translation } }); }

        private static int Unit(string directory)
        {
            Directory.CreateDirectory(directory);
            Caption caption;
            Check(CaptionParser.TryParse(Payload("RECEIVED", "Could you say that again?", "你能再说一遍吗？"), out caption), "Received caption must be accepted");
            Check(caption.Original == "Could you say that again?" && caption.Translation == "你能再说一遍吗？", "Bilingual text must survive Unicode round trip");
            foreach (var type in new[] { "SENT", "CHAT", "SETTINGS_UPDATED", "", "received" })
                Check(!CaptionParser.TryParse(Payload(type, "My own outgoing words", "我的话"), out caption), "Ignore non-received message: " + type);
            foreach (var input in new[] { "not json", "null", "[]", "{}", "{\"type\":\"RECEIVED\",\"message\":\"\",\"translation\":[]}" })
                Check(!CaptionParser.TryParse(input, out caption), "Ignore empty or malformed message");
            Check(!CaptionParser.TryParse("{\"type\":\"RECEIVED\",\"source\":\"ocr\",\"message\":\"HUD\"}", out caption), "Ignore OCR when a source tag is available");
            Check(CaptionParser.TryParse("{\"type\":\"RECEIVED\",\"message\":\"Hello\",\"translation\":[]}", out caption) && caption.Translation == "", "Show source while translation is off");
            Check(CaptionParser.TryParse("{\"type\":\"RECEIVED\",\"message\":\"<script>text only</script>\",\"translation\":[{\"message\":\"第一行\\n第二行\"}]}", out caption) && caption.Translation.Contains("\n"), "Accept multiline text without executing markup");
            Check(CaptionParser.TryParse(Payload("RECEIVED", new string('x', 15000), "你好"), out caption) && caption.Original.Length == 12001, "Bound display size");
            var file = Path.Combine(directory, "settings-test.json");
            JsonFiles.Write(file, new { WEBSOCKET_SERVER = true, WEBSOCKET_HOST = "127.0.0.1", WEBSOCKET_PORT = 2231, WEBSOCKET_AUTH_TOKEN = "unit secret&?中文" });
            var settings = ConnectionSettings.Load(file);
            Check(settings.Enabled && settings.Address.IsLoopback && settings.Address.Port == 2231, "Connect only to local VRCT endpoint");
            Check(settings.Address.Query.Contains("token=unit%20secret"), "Escape authentication token");
            JsonFiles.Write(file, new { WEBSOCKET_SERVER = false, WEBSOCKET_HOST = "localhost", WEBSOCKET_PORT = 2231, WEBSOCKET_AUTH_TOKEN = "test" });
            Check(!ConnectionSettings.Load(file).Enabled, "Recognize disabled interface");
            foreach (var host in new[] { "example.com", "192.168.1.2", "0.0.0.0" })
            {
                JsonFiles.Write(file, new { WEBSOCKET_SERVER = true, WEBSOCKET_HOST = host, WEBSOCKET_PORT = 2231, WEBSOCKET_AUTH_TOKEN = "test" });
                bool refused = false; try { ConnectionSettings.Load(file); } catch (InvalidDataException) { refused = true; }
                Check(refused, "Reject nonlocal host");
            }
            var state = Path.Combine(directory, "state-test.json");
            JsonFiles.Write(state, new { width = 740, fontSize = 22, topmost = true });
            JsonFiles.Write(state, new { width = 820, fontSize = 26, topmost = false });
            var saved = JsonFiles.Read(state);
            Check(JsonFiles.Number(saved, "width", 0) == 820 && JsonFiles.Number(saved, "fontSize", 0) == 26 && !JsonFiles.Flag(saved, "topmost", true), "Persist window preferences atomically");
            Console.WriteLine("PASS: " + assertions + " protocol/config/state assertions");
            return 0;
        }

        private static int Integration(string config, string reportFile, bool probeOnly)
        {
            var captions = new List<Caption>(); var connections = 0;
            using (var complete = new ManualResetEventSlim(false))
            using (var client = new CaptionClient(config))
            {
                client.ConnectionChanged += notice =>
                { if (notice.Connected) { Interlocked.Increment(ref connections); if (probeOnly) complete.Set(); } };
                client.CaptionReceived += caption => { lock (captions) { captions.Add(caption); if (captions.Count >= 2) complete.Set(); } };
                client.Start();
                Check(complete.Wait(TimeSpan.FromSeconds(25)), "Local subtitle connection timed out; connected=" + connections + "; category=" + client.LastFailureCategory);
                if (!probeOnly)
                {
                    lock (captions)
                    {
                        Check(captions.Count == 2, "Only received captions must reach window");
                        Check(captions[0].Original == "Hello learner!" && captions[0].Translation == "你好，学习者！", "Decode fragmented Unicode frame");
                        Check(captions[1].Original == "Take your time." && captions[1].Translation == "慢慢来。", "Reconnect and receive next caption");
                        Check(connections >= 2, "Reconnect after server disconnect");
                    }
                }
                JsonFiles.Write(reportFile, new { connected = true, connectionCount = connections, receivedCount = captions.Count, probeOnly = probeOnly });
                Console.WriteLine(probeOnly ? "PASS: live VRCT connection" : "PASS: authentication, filtering, fragmentation and reconnect");
            }
            return 0;
        }

        public static int Main(string[] args)
        {
            try
            {
                if (args.Length >= 3 && (args[0] == "--integration" || args[0] == "--probe")) return Integration(args[1], args[2], args[0] == "--probe");
                return Unit(args.Length > 0 ? args[0] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-output"));
            }
            catch (Exception error) { Console.Error.WriteLine("FAIL: " + error.Message); return 1; }
        }
    }
}
