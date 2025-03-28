using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Lab1
{
    public class TweetData
    {
        public double[] Coordinates { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string TweetText { get; set; }
        public string StateCode { get; set; }
        public double sentiment { get; set; }

        public static TweetData Parse(string line)
        {
            // Регулярное выражение для извлечения данных
            Regex regex = new Regex(@"\[(?<lat>-?\d+\.\d+),\s*(?<lon>-?\d+\.\d+)\]\s+_\s+(?<date>\d{4}-\d{2}-\d{2})\s+(?<time>\d{2}:\d{2}:\d{2})\s+(?<text>.+)");

            Match match = regex.Match(line);
            if (match.Success)
            {
                double latitude = double.Parse(match.Groups["lat"].Value, CultureInfo.InvariantCulture);
                double longitude = double.Parse(match.Groups["lon"].Value, CultureInfo.InvariantCulture);
                string Date = match.Groups["date"].Value;
                string Time = match.Groups["time"].Value;
                string tweetText = match.Groups["text"].Value;

                string stateCode = "XX";
                double Sentiment = 0.0;

                return new TweetData
                {
                    Coordinates = new double[] { latitude, longitude },
                    time = Time,
                    date = Date,
                    TweetText = tweetText,
                    StateCode = stateCode,
                    sentiment = Sentiment
                };
            }
            return null;
        }


        // Сериализация данных и сохранение в json
        public static void SerializeTweet(List<List<TweetData>> allTweets, string inputFolder, string outputFolder)
        {
            string[] txtFiles = Directory.GetFiles(inputFolder, "*.txt");

            for (int i = 0; i < txtFiles.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(txtFiles[i]);
                string jsonPath = Path.Combine(outputFolder, $"{fileName}.json");

                string jsonOutput = JsonConvert.SerializeObject(allTweets[i], Formatting.Indented);
                File.WriteAllText(jsonPath, jsonOutput);
            }
        }
    }
}
