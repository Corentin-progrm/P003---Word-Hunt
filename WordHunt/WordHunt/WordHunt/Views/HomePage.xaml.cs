using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Foundation;


namespace WordHunt.Views
{
    public sealed partial class HomePage : Page
    {
        // Collections pour les listes
        private ObservableCollection<string> foundWords = new ObservableCollection<string>();
        private ObservableCollection<string> triedWords = new ObservableCollection<string>();

        // Liste des mots du dictionnaire
        private List<WordItem> dictionary = new List<WordItem>();

        // Propriétés pour le suivi du progrès
        private int totalWords = 0;
        private int foundWordsCount = 0;

        public HomePage()
        {
            this.InitializeComponent();

            // Configuration des ListView
            FoundWordsList.ItemsSource = foundWords;
            TriedWordsList.ItemsSource = triedWords;

            // Gestion de la touche Enter dans l'AutoSuggestBox
            SearchBox.QuerySubmitted += SearchBox_QuerySubmitted;
            SearchBox.KeyDown += SearchBox_KeyDown;

            // Charger le dictionnaire
            LoadDictionary();
            UpdateProgress();
        }

        private void LoadDictionary()
        {
            try
            {
                // Utiliser la même méthode que votre code qui fonctionne
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "dictionary.json");

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    dictionary = JsonSerializer.Deserialize<List<WordItem>>(json) ?? new List<WordItem>();
                    totalWords = dictionary.Count;

                    // Compter les mots déjà trouvés et remplir la liste
                    foundWordsCount = 0;
                    foundWords.Clear();

                    foreach (var word in dictionary.Where(w => w.found))
                    {
                        foundWords.Insert(0, word.word); // Derniers trouvés en premier
                        foundWordsCount++;
                    }

                    ErrorMessage.Text = $"LOG : Dictionnaire chargé: {totalWords} mots";
                }
                else
                {
                    ErrorMessage.Text = "LOG : Fichier dictionary.json introuvable dans Assets";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"LOG : Erreur chargement: {ex.Message}";
            }
        }

        private void SaveDictionary()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "dictionary.json");
                string json = JsonSerializer.Serialize(dictionary, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);

                ErrorMessage.Text = "LOG : Sauvegardé !";
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"LOG : Erreur sauvegarde: {ex.Message}";
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            CheckWord();
        }

        private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                CheckWord();
            }
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            CheckWord();
        }

        private void CheckWord()
        {
            // Effacer le message d'erreur précédent
            ErrorMessage.Text = "";

            // Récupérer et nettoyer l'entrée utilisateur
            string userInput = SearchBox.Text?.Trim() ?? "";

            // Vérifier si l'entrée est vide
            if (string.IsNullOrEmpty(userInput))
            {
                return;
            }

            // Normaliser le mot pour la comparaison (supprimer accents et mettre en minuscules)
            string normalizedInput = NormalizeWord(userInput);

            // Chercher le mot dans le dictionnaire
            var foundWord = dictionary.FirstOrDefault(w =>
                NormalizeWord(w.word).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase));

            if (foundWord == null)
            {
                // Mot n'existe pas dans le dictionnaire
                ErrorMessage.Text = "LOG : Le mot n'existe pas dans le dictionnaire";

                // Ajouter à la liste des mots essayés s'il n'y est pas déjà
                if (!triedWords.Contains(userInput, StringComparer.OrdinalIgnoreCase))
                {
                    triedWords.Insert(0, userInput); // Insérer au début de la liste
                }
            }
            else
            {
                if (foundWord.found)
                {
                    // Mot déjà trouvé
                    ErrorMessage.Text = "LOG : Ce mot a déjà été trouvé";
                }
                else
                {
                    // Nouveau mot trouvé !
                    foundWord.found = true;
                    foundWordsCount++;
                    SearchBox.Text = "";

                    // Ajouter à la liste des mots trouvés
                    foundWords.Insert(0, foundWord.word); // Insérer au début de la liste

                    // Sauvegarder immédiatement
                    SaveDictionary();

                    // Mettre à jour le progrès
                    UpdateProgress();
                }
            }
        }

        private string NormalizeWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return "";

            // Convertir en minuscules et supprimer les accents
            string normalized = word.ToLowerInvariant();
            normalized = RemoveAccents(normalized);
            return normalized;
        }

        private string RemoveAccents(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private void UpdateProgress()
        {
            if (totalWords == 0) return;

            // Calculer le pourcentage
            double percentage = (double)foundWordsCount / totalWords * 100;

            // Mettre à jour les textes
            ProgressPercent.Text = $"{percentage:F2}%";
            Counter.Text = $"{foundWordsCount} / {totalWords}";

            // Mettre à jour l'arc
            DrawProgressArc(percentage);
        }

        private void DrawProgressArc(double percentage)
        {
            double radius = 56; // rayon = moitié du Canvas
            double centerX = 60;
            double centerY = 60;

            double angle = percentage / 100 * 360;

            // Convertir en radians
            double radians = (Math.PI / 180) * (angle - 90); // -90 pour commencer en haut

            double x = centerX + radius * Math.Cos(radians);
            double y = centerY + radius * Math.Sin(radians);

            bool isLargeArc = angle > 180;

            PathFigure figure = new PathFigure();
            figure.StartPoint = new Point(centerX, centerY - radius); // Point de départ en haut

            ArcSegment arc = new ArcSegment();
            arc.Point = new Point(x, y);
            arc.Size = new Size(radius, radius);
            arc.IsLargeArc = isLargeArc;
            arc.SweepDirection = SweepDirection.Clockwise;

            PathSegmentCollection segments = new PathSegmentCollection();
            segments.Add(arc);

            figure.Segments = segments;

            PathFigureCollection figures = new PathFigureCollection();
            figures.Add(figure);

            PathGeometry geometry = new PathGeometry();
            geometry.Figures = figures;

            ProgressArc.Data = geometry;
        }

    }

    // Classe pour représenter un mot du dictionnaire
    public class WordItem
    {
        public string word { get; set; } = "";
        public bool found { get; set; } = false;
    }
}