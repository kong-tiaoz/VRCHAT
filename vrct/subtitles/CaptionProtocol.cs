using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace VrcEnglishSubtitles
{
    public sealed class Caption
    {
        public string Original { get; set; }
        public string Translation { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    public static class JsonFiles
    {
        public static JavaScriptSerializer Serializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = 1048576, RecursionLimit = 32 };
        }

        public static Dictionary<string, object> Read(string file)
        {
            // VRCT replaces its configuration atomically. Allow replacement while reading.
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read,
                       FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                return Serializer().DeserializeObject(reader.ReadToEnd()) as Dictionary<string, object>;
        }

        public static string Text(IDictionary<string, object> data, string key)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) ? value as string : null;
        }

        public static bool Flag(IDictionary<string, object> data, string key, bool fallback)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) && value is bool ? (bool)value : fallback;
        }

        public static double Number(IDictionary<string, object> data, string key, double fallback)
        {
            object value;
            double number;
            if (data == null || !data.TryGetValue(key, out value) || value == null ||
                !double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out number) ||
                double.IsNaN(number) || double.IsInfinity(number)) return fallback;
            return number;
        }

        public static void Write(string file, object data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file)));
            var temporary = file + ".tmp";
            File.WriteAllText(temporary, Serializer().Serialize(data), new UTF8Encoding(false));
            if (File.Exists(file)) File.Replace(temporary, file, null);
            else File.Move(temporary, file);
        }
    }

    public static class CaptionParser
    {
        public static bool TryParse(string json, out Caption caption)
        {
            caption = null;
            try
            {
                var data = JsonFiles.Serializer().DeserializeObject(json) as Dictionary<string, object>;
                // Never mirror microphone/chat messages into this receive-only window.
                if (data == null || !string.Equals(JsonFiles.Text(data, "type"), "RECEIVED", StringComparison.Ordinal)) return false;
                var source = JsonFiles.Text(data, "source");
                if (!string.IsNullOrEmpty(source) && !string.Equals(source, "speaker", StringComparison.OrdinalIgnoreCase)) return false;
                var original = Clean(JsonFiles.Text(data, "message"), 12000);
                object rawTranslations;
                var translated = new List<string>();
                if (data.TryGetValue("translation", out rawTranslations))
                {
                    var text = rawTranslations as string;
                    if (text != null) translated.Add(text);
                    else
                    {
                        var items = rawTranslations as IEnumerable;
                        if (items != null)
                            foreach (var item in items)
                            {
                                var line = item as string;
                                var entry = item as Dictionary<string, object>;
                                if (line == null && entry != null) line = JsonFiles.Text(entry, "message");
                                if (!string.IsNullOrWhiteSpace(line)) translated.Add(line);
                            }
                    }
                }
                var translation = Clean(string.Join("\n", translated), 18000);
                if (string.IsNullOrWhiteSpace(original) && string.IsNullOrWhiteSpace(translation)) return false;
                caption = new Caption { Original = original, Translation = translation, ReceivedAt = DateTime.Now };
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        private static string Clean(string text, int maximum)
        {
            text = (text ?? "").Replace("\0", "").Trim();
            if (text.Length <= maximum) return text;
            var length = maximum;
            if (char.IsHighSurrogate(text[length - 1])) length--;
            return text.Substring(0, length) + "…";
        }
    }

    public sealed class ConnectionSettings
    {
        public bool Enabled { get; private set; }
        public Uri Address { get; private set; }

        public static ConnectionSettings Load(string configFile)
        {
            var data = JsonFiles.Read(configFile);
            if (data == null) throw new InvalidDataException("Invalid configuration");
            var host = JsonFiles.Text(data, "WEBSOCKET_HOST") ?? "127.0.0.1";
            if (!string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) && host != "::1")
                throw new InvalidDataException("Only local subtitle connections are supported");
            var portNumber = JsonFiles.Number(data, "WEBSOCKET_PORT", 2231);
            if (portNumber < 1 || portNumber > 65535 || Math.Truncate(portNumber) != portNumber)
                throw new InvalidDataException("Invalid local port");
            var token = JsonFiles.Text(data, "WEBSOCKET_AUTH_TOKEN");
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidDataException("Missing local token");
            var address = new UriBuilder("ws", host == "localhost" ? "127.0.0.1" : host, (int)portNumber, "/");
            address.Query = "token=" + Uri.EscapeDataString(token);
            return new ConnectionSettings { Enabled = JsonFiles.Flag(data, "WEBSOCKET_SERVER", false), Address = address.Uri };
        }
    }
}
