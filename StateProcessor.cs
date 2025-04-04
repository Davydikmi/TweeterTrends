using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NetTopologySuite.Geometries;
using Lab1;

public class StateProcessor
{
    public Dictionary<string, List<Polygon>> stateBoundaries; // Границы штатов
    private GeometryFactory geometryFactory; // Фабрика геометрических объектов

    public static readonly string[] StateCodes =
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
        "DC"
    };

    // Конструктор. Загружает границы штатов из JSON файла
    public StateProcessor(string filePath)
    {
        geometryFactory = new GeometryFactory();
        string jsonContent = File.ReadAllText(filePath);
        stateBoundaries = ParseJson(jsonContent);
    }

    // Заполняет словарь ключами-кодами штатов и значением по умолчанию
    public static void FillDictionaryWithKeys<T>(Dictionary<string, T> stateDictionary, T defaultValue)
    {
        foreach (string state in StateCodes)
        {
            stateDictionary.TryAdd(state, defaultValue);
        }
    }

    // Парсинг JSON файла с границами штатов
    private Dictionary<string, List<Polygon>> ParseJson(string jsonContent)
    {
        Dictionary<string, List<Polygon>> result = new Dictionary<string, List<Polygon>>();
        JObject json = JObject.Parse(jsonContent);

        foreach (var state in json)
        {
            List<Polygon> polygons = ProcessPart(state.Value, geometryFactory);
            if (polygons.Count > 0)
            {
                result[state.Key] = polygons;
            }
        }
        return result;
    }

    // Обработка массива координат и преобразование в полигоны
    public static List<Polygon> ProcessPart(JToken token, GeometryFactory gf)
    {
        List<Polygon> polygons = new List<Polygon>();

        if (token is JArray arr)
        {
            bool isSingleRing = AllCoordinatePairs(arr);
            if (isSingleRing && arr.Count >= 3)
            {
                // Простой случай: одна линия (кольцо) — создаём полигон
                Coordinate[] coords = ConvertToCoordinates(arr);
                polygons.Add(gf.CreatePolygon(coords));
                return polygons;
            }

            // Случай с несколькими кольцами:
            // Первое кольцо — внешняя граница, остальные — дырки внутри полигона
            bool isMultiRing = true;
            foreach (var ring in arr)
            {
                if (!(ring is JArray ringArr) || ringArr.Count < 3 || !AllCoordinatePairs(ringArr))
                {
                    isMultiRing = false;
                    break;
                }
            }

            if (isMultiRing)
            {
                // Первое кольцо — внешняя граница
                JArray exteriorArr = (JArray)arr[0];
                Coordinate[] exterior = ConvertToCoordinates(exteriorArr);

                // Остальные кольца — дырки внутри полигона
                List<LinearRing> holes = new List<LinearRing>();
                for (int i = 1; i < arr.Count; i++)
                {
                    JArray holeArr = (JArray)arr[i];
                    holes.Add(gf.CreateLinearRing(ConvertToCoordinates(holeArr)));
                }

                polygons.Add(gf.CreatePolygon(gf.CreateLinearRing(exterior), holes.ToArray()));
                return polygons;
            }

            // Если структура ещё сложнее (например, несколько полигонов) — вызов рекурсии
            foreach (var item in arr)
            {
                polygons.AddRange(ProcessPart(item, gf));
            }
        }

        return polygons;
    }


    // Проверка, что все элементы массива — пары координат
    public static bool AllCoordinatePairs(JArray arr)
    {
        foreach (JToken item in arr)
        {
            if (!IsCoordinatePair(item)) return false;
        }
        return true;
    }

    // Проверка, является ли элемент парой координат
    public static bool IsCoordinatePair(JToken token)
    {
        return token is JArray arr && arr.Count == 2 &&
               (arr[0].Type == JTokenType.Float || arr[0].Type == JTokenType.Integer) &&
               (arr[1].Type == JTokenType.Float || arr[1].Type == JTokenType.Integer);
    }

    // Конвертация массива пар координат в массив объектов Coordinate
    public static Coordinate[] ConvertToCoordinates(JArray arr)
    {
        List<Coordinate> coords = new List<Coordinate>();
        foreach (JToken item in arr)
        {
            if (IsCoordinatePair(item))
            {
                double x = item[0].ToObject<double>();
                double y = item[1].ToObject<double>();
                coords.Add(new Coordinate(x, y));
            }
        }
        return coords.ToArray();
    }

    // Присваивание кода штата каждому твиту на основе координат
    public void AssignStateCodes(List<List<TweetData>> allTweets)
    {
        foreach (List<TweetData> tweetList in allTweets)
        {
            foreach (TweetData tweet in tweetList)
            {
                tweet.StateCode = GetStateCode(tweet.Coordinates[1], tweet.Coordinates[0]);
            }
        }
    }

    // Получение кода штата по координатам твита
    private string GetStateCode(double lon, double lat)
    {
        Point point = geometryFactory.CreatePoint(new Coordinate(lon, lat));

        foreach (var state in stateBoundaries)
        {
            foreach (Polygon polygon in state.Value)
            {
                if (polygon.Contains(point))
                {
                    return state.Key;
                }
            }
        }
        return "XX";
    }
}
