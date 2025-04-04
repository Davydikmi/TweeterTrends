using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using NetTopologySuite.Geometries;
using NetTopologySuite;
using Newtonsoft.Json.Linq;
using Lab1;

namespace TwitterTrends.Services
{
    public static class MapService
    {
        private static readonly int MapWidth = 1920; 
        private static readonly int MapHeight = 1080; 

        // Отрисовка карты
        public static void DrawMap(List<Dictionary<string, double>> AvrStateSentiments, Dictionary<string, List<Polygon>> statePolygons, string outputPath)
        {
            string[] txtFiles = Directory.GetFiles(@"..\..\..\InputData", "*.txt");

            // Отрисовка карты для каждого файла
            for (int i = 0; i < AvrStateSentiments.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(txtFiles[i]) + ".png";
                string outputFilePath = Path.Combine(outputPath, fileName);
                DrawSingleMap(AvrStateSentiments[i], statePolygons, outputFilePath);
            }
        }

        // Отрисовка карты для каждого файла
        private static void DrawSingleMap(Dictionary<string, double> stateSentiments, Dictionary<string, List<Polygon>> statePolygons, string outputFilePath)
        {
            using (Bitmap bitmap = new Bitmap(MapWidth, MapHeight))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                Dictionary<string, string> stateColors = CalculateStateColors(stateSentiments); // получение цветов по настроению

                // Отрисовка каждого штата
                foreach (var state in statePolygons)
                {
                    string stateCode = state.Key;
                    Color color = ColorTranslator.FromHtml(stateColors.ContainsKey(stateCode) ? stateColors[stateCode] : "#FFFFFF");

                    foreach (var polygon in state.Value)
                    {
                        DrawPolygon(graphics, polygon, color); // отрисовка полигона (границы штата)
                    }

                    // Подпись кода штата по центру полигона
                    if (stateSentiments.ContainsKey(stateCode))
                    {
                        var textPosition = GetPolygonCentroid(state.Value[0]);
                        graphics.DrawString(stateCode, new Font("Arial", 10, FontStyle.Bold), Brushes.Black, textPosition);
                    }
                }

                // Добавление легенды с средним настроением по каждому штату
                DrawLegend(graphics, stateSentiments);

                bitmap.Save(outputFilePath);
                Console.WriteLine("Карта сохранена: " + outputFilePath);
            }
        }

        // Расчет цвета для каждого штата на основе среднего настроения
        public static Dictionary<string, string> CalculateStateColors(Dictionary<string, double> stateSentiments)
        {
            Dictionary<string,string> stateColors = new Dictionary<string, string>();
            double minSentiment = stateSentiments.Values.Min();
            double maxSentiment = stateSentiments.Values.Max();

            foreach (var state in stateSentiments)
            {
                double sentiment = Math.Round(state.Value, 6);
                string color;

                if (sentiment == 0) 
                {
                    color = "#808080"; // серый
                }
                else if (sentiment < 0)
                {
                    double ratio = (sentiment - minSentiment) / (0 - minSentiment); // нормализация
                    string[] negativeColor = { "#FFA500", "#FF4500", "#FF0000", "#8B0000" }; // от оранжевого к красному
                    color = negativeColor[(int)(ratio * (negativeColor.Length - 1))];
                }
                else
                {
                    double ratio = (sentiment - 0) / (maxSentiment - 0);
                    string[] positiveColor = { "#A8FFB0", "#50FA7B", "#00CED1", "#1E90FF" }; // от зелёного к синему
                    color = positiveColor[(int)(ratio * (positiveColor.Length - 1))];
                }
                stateColors[state.Key] = color;
            }
            return stateColors;
        }

        // Отрисовка полигона (границ одного штата)
        private static void DrawPolygon(Graphics graphics, Geometry polygon, Color color)
        {
            if (polygon is Polygon poly)
            {
                var exteriorCoords = poly.ExteriorRing.Coordinates;
                PointF[] points = exteriorCoords.Select(coord => new PointF(
                    (float)((coord.X + 170) / (104) * MapWidth),  // преобразование долготы
                    (float)((72 - coord.Y) / (57) * MapHeight)    // преобразование широты
                )).ToArray();

                using (Brush brush = new SolidBrush(color))
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    graphics.FillPolygon(brush, points);
                    graphics.DrawPolygon(pen, points);
                }
            }
        }

        // Получение центра полигона для подписи кода штата
        private static PointF GetPolygonCentroid(Geometry polygon)
        {
            var centroid = polygon.Centroid.Coordinate;
            return new PointF(
                (float)((centroid.X + 170) / (104) * MapWidth),
                (float)((72 - centroid.Y) / (57) * MapHeight)
            );
        }

        // Отображение легенды с значениями среднего настроения по каждому штату
        private static void DrawLegend(Graphics graphics, Dictionary<string, double> stateSentiments)
        {
            Font font = new Font("Arial", 11);
            Brush brush = Brushes.Black;
            int x = 10, y = MapHeight - 150;
            int columnWidth = 150;
            int count = 0;

            foreach (var state in stateSentiments.OrderBy(s => s.Key))
            {
                string text = $"{state.Key}: {state.Value:F4}";
                graphics.DrawString(text, font, brush, x, y);
                y += 15;
                count++;
                if (count % 10 == 0) // перенос в новый столбец каждые 10 строк
                {
                    x += columnWidth;
                    y = MapHeight - 150;
                }
            }
        }
    }
}
