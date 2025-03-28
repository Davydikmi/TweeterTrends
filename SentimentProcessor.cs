using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Lab1;
using NetTopologySuite.Noding;

public class SentimentProcessor
{
    // Словарь для хранения слов и их оценки настроения
    private Dictionary<string, double> sentimentDictionary;

    // Конструктор: загружает словарь настроений из файла
    public SentimentProcessor(string sentimentFile)
    {
        sentimentDictionary = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        LoadSentiments(sentimentFile);
    }

    // Загрузка словаря настроений из файла
    private void LoadSentiments(string filePath)
    {
        foreach (string line in File.ReadLines(filePath))
        {
            string[] parts = line.Split(',', 2);
            if (parts.Length == 2)
            {
                string phrase = parts[0].Trim().ToLower();
                if (double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double score))
                {
                    sentimentDictionary[phrase] = score;
                }
            }
        }
    }

    // Подсчет оценки настроения для каждого твита
    public void ProcessTweets(List<List<TweetData>> allTweets)
    {
        foreach (var tweetList in allTweets)
        {
            foreach (var tweet in tweetList)
            {
                List<string> words = ExtractWords(tweet.TweetText);
                double sentimentScore = 0.0;

                for (int i = 0; i < words.Count; i++)
                {
                    string word = words[i];
                    sentimentScore += GetSentiment(word);

                    // Проверка на возможные словосочетания длиной 2-4 слова
                    for (int j = 2; j <= 4; j++)
                    {
                        if (i + j <= words.Count)
                        {
                            string phrase = string.Join(" ", words.Skip(i).Take(j));
                            double phraseSentiment = GetSentiment(phrase);
                            if (phraseSentiment != 0)
                            {
                                sentimentScore += phraseSentiment;
                                i += (j - 1);
                                break;
                            }
                        }
                    }
                }

                tweet.sentiment = sentimentScore; 
            }
        }
    }

    // Получение оценки настроения для слова или фразы
    private double GetSentiment(string phrase)
    {
        return sentimentDictionary.TryGetValue(phrase, out double score) ? score : 0.0;
    }

    // Разделение текста твита на отдельные слова
    private List<string> ExtractWords(string text)
    {
        List<string> words = new List<string>();

        foreach (Match match in Regex.Matches(text.ToLower(), @"\b[a-zA-Z`-]+\b"))
        {
            string word = match.Value.Replace("`", ""); // удаляем ` для can`t и других подобных
            if (!word.StartsWith("http") && !word.StartsWith("@")) // игнорируем ссылки и упоминания
            {
                words.Add(word);
            }
        }

        return words;
    }

    // Подсчет среднего настроения по каждому штату и каждому файлу
    public List<Dictionary<string, double>> AvrSentimentCount(List<List<TweetData>> allTweets, string folderPath)
    {
        List<Dictionary<string, double>> AvrStateSentiment = new List<Dictionary<string, double>>(); // Список словарей со средним настроением по каждому штату
        List<Dictionary<string, int>> StateTweetCount = new List<Dictionary<string, int>>(); // Список словарей с количеством твитов по каждому штату

        string[] txtFiles = Directory.GetFiles(folderPath, "*.txt");

        for (int i = 0; i < txtFiles.Length; i++)
        {
            var sentimentDict = new Dictionary<string, double>();
            var countDict = new Dictionary<string, int>();

            // Заполняем словари кодами штатов 
            StateProcessor.FillDictionaryWithKeys(sentimentDict, 0.0);
            StateProcessor.FillDictionaryWithKeys(countDict, 0);

            AvrStateSentiment.Add(sentimentDict);
            StateTweetCount.Add(countDict);
        }

        // Подсчет суммы оценок и количества твитов по каждому штату
        for (int i = 0; i < allTweets.Count; i++)
        {
            foreach (var tweet in allTweets[i])
            {
                if (tweet.StateCode != "XX") 
                {
                    AvrStateSentiment[i][tweet.StateCode] += tweet.sentiment;
                    StateTweetCount[i][tweet.StateCode]++; 
                }
            }

            // Подсчет среднего значения настроения
            foreach (var state in AvrStateSentiment[i].Keys.ToList())
            {
                if (StateTweetCount[i][state] > 0)
                {
                    AvrStateSentiment[i][state] /= StateTweetCount[i][state];
                }
            }
        }

        return AvrStateSentiment;
    }
}
