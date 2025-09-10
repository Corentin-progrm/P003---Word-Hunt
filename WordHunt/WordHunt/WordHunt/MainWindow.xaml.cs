using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text.Json;
using Windows.Foundation;
using Windows.UI;
using WinRT.Interop;

namespace WordHunt
{
    public sealed partial class MainWindow : Window
    {
        private List<WordEntry> dictionary = new();

        public MainWindow()
        {
            this.InitializeComponent();

            // Maximiser la fenêtre
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }

            LoadDictionary();
            UpdateUI();
        }

        private void LoadDictionary()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "dictionary.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                dictionary = JsonSerializer.Deserialize<List<WordEntry>>(json) ?? new List<WordEntry>();
            }
        }

        private void SaveDictionary()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "dictionary.json");
            string json = JsonSerializer.Serialize(dictionary, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            string word = SearchBox.Text.Trim().ToLower();
            ErrorMessage.Text = "";

            if (string.IsNullOrEmpty(word)) return;

            var entry = dictionary.Find(w => w.word.ToLower() == word);

            if (entry == null)
            {
                ErrorMessage.Text = "❌ Ce mot n'est pas dans le dictionnaire.";
            }
            else if (entry.found)
            {
                ErrorMessage.Text = "⚠️ Vous avez déjà trouvé ce mot.";
            }
            else
            {
                entry.found = true;
                FoundWordsList.Items.Add(entry.word);
                SaveDictionary();
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            int total = dictionary.Count;
            int found = dictionary.FindAll(w => w.found).Count;

            Counter.Text = $"Found {found} / {total} words";

            UpdateDonut(found, total);
        }

        private void UpdateDonut(int found, int total)
        {
            double progress = total > 0 ? (double)found / total : 0;
            ProgressPercent.Text = $"{(int)(progress * 100)}%";

            double angle = 360 * progress;
            double radians = (angle - 90) * Math.PI / 180.0;
            double radius = 120;

            double startX = 150;
            double startY = 30;
            double endX = 150 + radius * Math.Cos(radians);
            double endY = 150 + radius * Math.Sin(radians);

            bool isLargeArc = angle > 180;

            PathFigure figure = new PathFigure
            {
                StartPoint = new Point(startX, startY),
                IsClosed = false
            };

            ArcSegment arc = new ArcSegment
            {
                Point = new Point(endX, endY),
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };

            figure.Segments.Clear();
            figure.Segments.Add(arc);

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            ProgressArc.Data = geometry;
        }
    }

    public class WordEntry
    {
        public string word { get; set; } = "";
        public bool found { get; set; } = false;
    }
}
