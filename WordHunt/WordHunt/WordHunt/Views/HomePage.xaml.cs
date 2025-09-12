using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

                    ErrorMessage.Text = $"Dictionnaire chargé: {totalWords} mots";
                }
                else
                {
                    ErrorMessage.Text = "Fichier dictionary.json introuvable dans Assets";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Erreur chargement: {ex.Message}";
            }
        }

        private void SaveDictionary()
        {
            try
            {
                // Utiliser la même méthode que votre code qui fonctionne
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "dictionary.json");
                string json = JsonSerializer.Serialize(dictionary, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);

                ErrorMessage.Text = "Sauvegardé !";
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Erreur sauvegarde: {ex.Message}";
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

            // Vérifier si l'entrée contient seulement des lettres et des accents
            if (!IsValidWord(userInput))
            {
                ErrorMessage.Text = "Le mot contient des caractères non valides";
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
                ErrorMessage.Text = "Le mot n'existe pas dans le dictionnaire";

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
                    ErrorMessage.Text = "Ce mot a déjà été trouvé";
                }
                else
                {
                    // Nouveau mot trouvé !
                    foundWord.found = true;
                    foundWordsCount++;

                    // Ajouter à la liste des mots trouvés
                    foundWords.Insert(0, foundWord.word); // Insérer au début de la liste

                    // Sauvegarder immédiatement
                    SaveDictionary();

                    // Mettre à jour le progrès
                    UpdateProgress();
                }
            }
        }

        private bool IsValidWord(string word)
        {
            // Regex pour vérifier que le mot contient seulement des lettres (avec accents)
            return Regex.IsMatch(word, @"^[a-zA-ZÀ-ÿ]+$");
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

            // Mettre à jour les textes - SEULEMENT le pourcentage et le compteur
            ProgressPercent.Text = $"{percentage:F3}%";
            Counter.Text = $"{foundWordsCount} / {totalWords}";
        }
    }

    // Classe pour représenter un mot du dictionnaire
    public class WordItem
    {
        public string word { get; set; } = "";
        public bool found { get; set; } = false;
    }
}