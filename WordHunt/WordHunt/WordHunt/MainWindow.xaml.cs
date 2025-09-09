using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Windows.UI;

namespace WordHunt
{
    public sealed partial class MainWindow : Window
    {
        private List<WordEntry> dictionary = new();

        public MainWindow()
        {
            this.InitializeComponent();
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
                ErrorMessage.Text = "❌ This word is not in the dictionary.";
            }
            else if (entry.found)
            {
                ErrorMessage.Text = "⚠️ You already found this word.";
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
            double radius = 70;

            double startX = 100;
            double startY = 30; // top of the circle
            double endX = 100 + radius * Math.Cos(radians);
            double endY = 100 + radius * Math.Sin(radians);

            bool isLargeArc = angle > 180;

            // Build arc geometry manually
            PathFigure figure = new PathFigure
            {
                StartPoint = new Windows.Foundation.Point(startX, startY),
                IsClosed = false
            };

            ArcSegment arc = new ArcSegment
            {
                Point = new Windows.Foundation.Point(endX, endY),
                Size = new Windows.Foundation.Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };

            figure.Segments.Add(arc);

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            ProgressArc.Data = geometry;
        }


        private async void Readme_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "About the Game",
                Content = "Word Finder Game\n\nAuthor: Ton Nom\nVersion: 1.0\n\nBut du jeu : trouver tous les mots du dictionnaire !",
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    public class WordEntry
    {
        public string word { get; set; } = "";
        public bool found { get; set; } = false;
    }
}
