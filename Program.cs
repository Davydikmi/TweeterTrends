using System;
using Lab1;
using TwitterTrends.Services;

class Program
{
    static void Main(string[] args)
    {
        List<List<TweetData>> allTweets = new List<List<TweetData>>();
        InitFile(allTweets, @"..\..\..\InputData");

        // Тестовый вариант
        //InitFile(allTweets, @"..\..\..\Test");

        SentimentProcessor processor = new SentimentProcessor(@"..\..\..\InputData\sentiments.csv");
        processor.ProcessTweets(allTweets);

        StateProcessor stateProcessor = new StateProcessor(@"..\..\..\InputData\states.json");
        stateProcessor.AssignStateCodes(allTweets);

        TweetData.SerializeTweet(allTweets, @"..\..\..\InputData", @"..\..\..\OutputData\json");

        List<Dictionary<string, double>> AvrStateSentiments = processor.AvrSentimentCount(allTweets, @"..\..\..\InputData");
        PrintAvrSentiments(AvrStateSentiments, @"..\..\..\InputData");

        MapService.DrawMap(AvrStateSentiments, @"..\..\..\InputData\states.json", @"..\..\..\OutputData\image");

        // Тестовый вариант
        //TweetData.SerializeTweet(allTweets, @"..\..\..\Test", @"..\..\..\Test");
        //List<Dictionary<string, double>> AvrStateSentiments = processor.AvrSentimentCount(allTweets, @"..\..\..\Test");
        //PrintAvrSentiments(AvrStateSentiments, @"..\..\..\Test");

        //MapService.DrawMap(AvrStateSentiments, @"..\..\..\InputData\states.json", @"..\..\..\Test");
    }


    // Инициализация файлов и их парсинг
    private static void InitFile(List<List<TweetData>> allTweets, string folderPath)
    {
        string[] txtFiles = Directory.GetFiles(folderPath, "*.txt");

        foreach (string txtFile in txtFiles)
        {
            List<TweetData> tweets = new List<TweetData>();
            foreach (string line in File.ReadLines(txtFile))
            {
                TweetData tweet = TweetData.Parse(line);
                if (tweet != null) tweets.Add(tweet);
            }

            allTweets.Add(tweets);
        }
    }


    // Вывод в консоль среднего настроения по штатам
    public static void PrintAvrSentiments(List<Dictionary<string, double>> avrStateSentiments, string folderPath)
    {
        string[] txtFiles = Directory.GetFiles(folderPath, "*.txt");

        for (int i = 0; i < avrStateSentiments.Count; i++)
        {
            Console.WriteLine($"File: {Path.GetFileName(txtFiles[i])}");

            foreach (var entry in avrStateSentiments[i])
            {
                Console.WriteLine($"{entry.Key}: {entry.Value:F4}");
            }

            Console.WriteLine(new string('-', 40));
        }
    }


}
