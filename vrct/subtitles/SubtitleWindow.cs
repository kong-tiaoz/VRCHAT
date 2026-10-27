using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace VrcEnglishSubtitles
{
    public sealed class SubtitleWindow : Window
    {
        private readonly string configFile;
        private readonly string stateFile;
        private readonly bool demo;
        private readonly List<TextBlock> englishBlocks = new List<TextBlock>();
        private readonly List<TextBlock> chineseBlocks = new List<TextBlock>();
        private readonly StackPanel rows = new StackPanel();
        private readonly ScrollViewer history = new ScrollViewer();
        private readonly TextBlock status = new TextBlock();
        private readonly TextBlock counter = new TextBlock();
        private readonly Border statusDot = new Border();
        private readonly Border panel = new Border();
        private readonly TextBlock empty = new TextBlock();
        private readonly CheckBox follow = new CheckBox();
        private readonly DispatcherTimer saveTimer = new DispatcherTimer();
        private CaptionClient client;
        private double fontSize = 20;
        private double panelAlpha = 0.94;
        private int total;
        private bool loaded;

        private static SolidColorBrush ColorBrush(string color)
        { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }

        public SubtitleWindow(string config, bool preview)
        {
            configFile = config;
            demo = preview;
            stateFile = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "state.json"));
            Title = preview ? "英语陪练字幕 · 演示" : "英语陪练字幕";
            Width = 740; Height = 330; MinWidth = 480; MinHeight = 220;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowActivated = false;
            Topmost = true;
            FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI");
            Foreground = ColorBrush("#ECF2FA");
            RestoreState();
            BuildLayout();
            saveTimer.Interval = TimeSpan.FromMilliseconds(450);
            saveTimer.Tick += delegate { saveTimer.Stop(); SaveState(); };
            LocationChanged += delegate { QueueSave(); };
            SizeChanged += delegate { QueueSave(); };
            Closing += delegate { saveTimer.Stop(); if (client != null) client.Dispose(); SaveState(); };
            Loaded += OnLoaded;
        }

        private Button Tool(string text, string hint, RoutedEventHandler handler)
        {
            var button = new Button { Content = text, ToolTip = hint, Height = 29,
                MinWidth = 32, Padding = new Thickness(7, 0, 7, 0), Margin = new Thickness(3, 0, 0, 0),
                Background = ColorBrush("#273447"), Foreground = ColorBrush("#DCE8F7"),
                BorderBrush = ColorBrush("#3A4B62"), BorderThickness = new Thickness(1), FontSize = 12 };
            if (handler != null) button.Click += handler;
            System.Windows.Automation.AutomationProperties.SetName(button, hint);
            return button;
        }

        private void BuildLayout()
        {
            panel.CornerRadius = new CornerRadius(12);
            panel.BorderBrush = ColorBrush("#40536B"); panel.BorderThickness = new Thickness(1);
            SetPanelAlpha(); Content = panel;
            var grid = new Grid(); panel.Child = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var header = new Grid { Margin = new Thickness(15, 11, 12, 10) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent };
            brand.Children.Add(new TextBlock { Text = "英语陪练", FontSize = 15, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center });
            brand.Children.Add(new TextBlock { Text = "  EN → 中文", Foreground = ColorBrush("#8ECDF4"),
                FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });
            brand.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            { if (e.ButtonState == MouseButtonState.Pressed) { try { DragMove(); } catch (InvalidOperationException) { } } };
            header.Children.Add(brand);
            var tools = new StackPanel { Orientation = Orientation.Horizontal };
            tools.Children.Add(Tool("A−", "缩小字幕字号", delegate { ChangeFont(-2); }));
            tools.Children.Add(Tool("A+", "放大字幕字号", delegate { ChangeFont(2); }));
            tools.Children.Add(Tool("清空", "清空本窗口字幕", delegate { ClearCaptions(); }));
            var pin = Tool(Topmost ? "已置顶" : "置顶", "切换字幕置顶", null);
            pin.Click += delegate { Topmost = !Topmost; pin.Content = Topmost ? "已置顶" : "置顶"; QueueSave(); };
            tools.Children.Add(pin);
            tools.Children.Add(Tool("—", "最小化字幕窗口", delegate { WindowState = WindowState.Minimized; }));
            tools.Children.Add(Tool("×", "关闭字幕窗口", delegate { Close(); }));
            Grid.SetColumn(tools, 1); header.Children.Add(tools); grid.Children.Add(header);
            var body = new Grid { Margin = new Thickness(12, 0, 12, 0) };
            history.Content = rows; history.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            history.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            history.ScrollChanged += delegate(object sender, ScrollChangedEventArgs e)
            { if (e.ExtentHeightChange == 0 && e.VerticalChange < 0) follow.IsChecked = false; };
            body.Children.Add(history);
            empty.Text = "等待对方说英语…\n英语原文和中文译文会显示在这里";
            empty.FontSize = 17; empty.Foreground = ColorBrush("#91A2B8");
            empty.TextAlignment = TextAlignment.Center; empty.LineHeight = 31;
            empty.HorizontalAlignment = HorizontalAlignment.Center; empty.VerticalAlignment = VerticalAlignment.Center;
            body.Children.Add(empty); Grid.SetRow(body, 1); grid.Children.Add(body);
            var footer = new Grid { Margin = new Thickness(16, 10, 17, 13) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var statusGroup = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            statusGroup.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusGroup.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusDot.Width = 7; statusDot.Height = 7; statusDot.CornerRadius = new CornerRadius(4);
            statusDot.Background = ColorBrush("#D8B975"); statusDot.Margin = new Thickness(0, 0, 7, 0);
            statusDot.VerticalAlignment = VerticalAlignment.Center; statusGroup.Children.Add(statusDot);
            status.Text = "正在准备字幕…"; status.FontSize = 11;
            status.Foreground = ColorBrush("#A7B8CC"); status.TextTrimming = TextTrimming.CharacterEllipsis;
            Grid.SetColumn(status, 1); statusGroup.Children.Add(status); footer.Children.Add(statusGroup);
            var preferences = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            follow.Content = "跟随最新"; follow.FontSize = 11; follow.IsChecked = true;
            follow.Foreground = ColorBrush("#A7B8CC"); follow.VerticalAlignment = VerticalAlignment.Center;
            follow.Click += delegate { if (follow.IsChecked == true) history.ScrollToEnd(); };
            preferences.Children.Add(follow);
            var opacity = new Slider { Minimum = 0.5, Maximum = 1, Value = panelAlpha, Width = 64,
                ToolTip = "字幕背景不透明度", Margin = new Thickness(13, 0, 13, 0), VerticalAlignment = VerticalAlignment.Center };
            opacity.ValueChanged += delegate { panelAlpha = opacity.Value; SetPanelAlpha(); QueueSave(); };
            preferences.Children.Add(opacity);
            counter.Text = "仅自己可见"; counter.FontSize = 11; counter.Foreground = ColorBrush("#8ECDF4");
            counter.VerticalAlignment = VerticalAlignment.Center; preferences.Children.Add(counter);
            Grid.SetColumn(preferences, 1); footer.Children.Add(preferences); Grid.SetRow(footer, 2); grid.Children.Add(footer);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            loaded = true;
            if (demo)
            {
                ShowConnection(new ConnectionNotice { Connected = true, Message = "演示预览 · 并非真实对话" });
                AddCaption(new Caption { Original = "Could you speak a little more slowly?", Translation = "你能说得稍微慢一点吗？", ReceivedAt = DateTime.Now });
                AddCaption(new Caption { Original = "Of course. Take your time — we can practice together.", Translation = "当然，慢慢来，我们可以一起练习。", ReceivedAt = DateTime.Now });
                return;
            }
            client = new CaptionClient(configFile);
            client.CaptionReceived += caption => Ui(delegate { AddCaption(caption); });
            client.ConnectionChanged += notice => Ui(delegate { ShowConnection(notice); });
            client.Start();
        }

        private void Ui(Action action)
        { if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(action); }

        private void ShowConnection(ConnectionNotice notice)
        {
            status.Text = notice.Message; status.ToolTip = notice.Message;
            statusDot.Background = ColorBrush(notice.Connected ? "#76D8B4" : "#D8B975");
        }

        private void AddCaption(Caption caption)
        {
            empty.Visibility = Visibility.Collapsed;
            var card = new Border { Background = ColorBrush("#202C3D"), CornerRadius = new CornerRadius(8),
                BorderBrush = ColorBrush("#314157"), BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 12), Margin = new Thickness(0, 0, 4, 8) };
            var content = new StackPanel(); card.Child = content;
            content.Children.Add(new TextBlock { Text = caption.ReceivedAt.ToString("HH:mm") + "  对方英语",
                FontSize = 10, Foreground = ColorBrush("#90A4BD"), Margin = new Thickness(0, 0, 0, 5) });
            var english = new TextBlock { Text = caption.Original, FontSize = fontSize,
                TextWrapping = TextWrapping.Wrap, Foreground = ColorBrush("#EFF4FC") };
            var chinese = new TextBlock { Text = string.IsNullOrWhiteSpace(caption.Translation) ? "（等待中文译文，请确认 VRCT 的翻译开关已打开）" : caption.Translation,
                FontSize = fontSize + 2, TextWrapping = TextWrapping.Wrap,
                Foreground = ColorBrush("#9BE5CB"), Margin = new Thickness(0, 6, 0, 0) };
            content.Children.Add(english); content.Children.Add(chinese);
            englishBlocks.Add(english); chineseBlocks.Add(chinese); rows.Children.Add(card);
            if (rows.Children.Count > 40) { rows.Children.RemoveAt(0); englishBlocks.RemoveAt(0); chineseBlocks.RemoveAt(0); }
            total++; counter.Text = "仅自己可见 · " + total + " 句";
            if (follow.IsChecked == true) Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate { history.ScrollToEnd(); }));
        }

        private void ClearCaptions()
        {
            rows.Children.Clear(); englishBlocks.Clear(); chineseBlocks.Clear(); total = 0;
            counter.Text = "仅自己可见"; empty.Visibility = Visibility.Visible;
        }

        private void ChangeFont(double amount)
        {
            fontSize = Math.Max(14, Math.Min(38, fontSize + amount));
            foreach (var block in englishBlocks) block.FontSize = fontSize;
            foreach (var block in chineseBlocks) block.FontSize = fontSize + 2;
            QueueSave();
        }

        private void SetPanelAlpha()
        { panel.Background = new SolidColorBrush(Color.FromArgb((byte)(255 * panelAlpha), 22, 31, 45)); }

        private void RestoreState()
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Left + (screen.Width - Width) / 2;
            Top = screen.Bottom - Height - 55;
            try
            {
                if (!File.Exists(stateFile) || demo) return;
                var data = JsonFiles.Read(stateFile);
                Width = Math.Max(MinWidth, Math.Min(1400, JsonFiles.Number(data, "width", Width)));
                Height = Math.Max(MinHeight, Math.Min(1000, JsonFiles.Number(data, "height", Height)));
                var left = JsonFiles.Number(data, "left", Left); var top = JsonFiles.Number(data, "top", Top);
                var saved = new Rect(left, top, Width, Height);
                var desktop = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
                var intersection = Rect.Intersect(saved, desktop);
                if (!intersection.IsEmpty && intersection.Width > 100 && intersection.Height > 80) { Left = left; Top = top; }
                Topmost = JsonFiles.Flag(data, "topmost", true);
                fontSize = Math.Max(14, Math.Min(38, JsonFiles.Number(data, "fontSize", 20)));
                panelAlpha = Math.Max(0.5, Math.Min(1, JsonFiles.Number(data, "opacity", 0.94)));
            }
            catch (Exception) { }
        }

        private void QueueSave()
        { if (loaded && !demo) { saveTimer.Stop(); saveTimer.Start(); } }

        private void SaveState()
        {
            if (!loaded || demo) return;
            try
            {
                var bounds = RestoreBounds;
                JsonFiles.Write(stateFile, new { left = bounds.Left, top = bounds.Top, width = bounds.Width,
                    height = bounds.Height, topmost = Topmost, fontSize = fontSize, opacity = panelAlpha });
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var config = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "app", "config.json"));
            var demo = false;
            var previewSeconds = 0;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config" && i + 1 < args.Length) config = Path.GetFullPath(args[++i]);
                else if (args[i] == "--demo") demo = true;
                else if (args[i] == "--exit-after" && i + 1 < args.Length) int.TryParse(args[++i], out previewSeconds);
            }
            string key;
            using (var hash = SHA256.Create()) key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(config.ToLowerInvariant() + demo))).Replace("-", "").Substring(0, 20);
            bool created;
            using (var singleton = new Mutex(true, "Local\\VrcEnglishSubtitles-" + key, out created))
            {
                if (!created)
                {
                    try { using (var existing = EventWaitHandle.OpenExisting("Local\\VrcEnglishSubtitlesActivate-" + key)) existing.Set(); }
                    catch (WaitHandleCannotBeOpenedException) { }
                    return 0;
                }
                using (var activate = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\VrcEnglishSubtitlesActivate-" + key))
                {
                    var application = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
                    var window = new SubtitleWindow(config, demo);
                    if (demo && previewSeconds > 0)
                    {
                        var previewTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(previewSeconds) };
                        previewTimer.Tick += delegate { previewTimer.Stop(); window.Close(); };
                        previewTimer.Start();
                    }
                    var registration = ThreadPool.RegisterWaitForSingleObject(activate, delegate
                    {
                        if (!application.Dispatcher.HasShutdownStarted) application.Dispatcher.BeginInvoke(new Action(delegate
                        { window.Show(); window.WindowState = WindowState.Normal; window.Activate(); }));
                    }, null, Timeout.Infinite, false);
                    try { return application.Run(window); }
                    finally { registration.Unregister(null); singleton.ReleaseMutex(); }
                }
            }
        }
    }
}
