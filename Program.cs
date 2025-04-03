using System;
using Lab1;
using TwitterTrends.Services;

class Program
{
    static void Main(string[] args)
    {
        // Импортирование твитов в список
        List<List<TweetData>> allTweets = new List<List<TweetData>>();
        InitFile(allTweets, @"..\..\..\InputData");

        // Подсчет настроения для каждого твита
        SentimentProcessor processor = new SentimentProcessor(@"..\..\..\InputData\sentiments.csv");
        processor.ProcessTweets(allTweets);

        // Определение кода штата для каждого твита
        StateProcessor stateProcessor = new StateProcessor(@"..\..\..\InputData\states.json");
        stateProcessor.AssignStateCodes(allTweets);

        // Сереализация твитов в json файл для просмотра содержимого
        TweetData.SerializeTweet(allTweets, @"..\..\..\InputData", @"..\..\..\OutputData\json");

        // Подсчет среднего настроения для каждого штата
        List<Dictionary<string, double>> AvrStateSentiments = processor.AvrSentimentCount(allTweets, @"..\..\..\InputData");
        PrintAvrSentiments(AvrStateSentiments, @"..\..\..\InputData");

        // Отрисовка карты и ее сохранение
        MapService.DrawMap(AvrStateSentiments, @"..\..\..\InputData\states.json", @"..\..\..\OutputData\image");
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
