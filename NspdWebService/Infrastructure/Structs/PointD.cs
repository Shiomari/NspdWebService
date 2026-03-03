using System.Text.Json.Serialization;

namespace NspdWebService.Infrastructure.Structs
{
    /// <summary>
    /// Структура для представления точки с координатами X и Y.
    /// </summary>
    public struct PointD
    {
        /// <summary>
        /// Инициализирует новую точку с заданными координатами.
        /// </summary>
        /// <param name="x">Координата X.</param>
        /// <param name="y">Координата Y.</param>
        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        /// <summary>
        /// Вычисляет центр масс (центроид) многоугольника.
        /// </summary>
        /// <param name="polygon">Массив точек многоугольника.</param>
        /// <returns>Центроид многоугольника.</returns>
        public static PointD GetCentroid(PointD[] polygon)
        {
            double area = 0;
            double centerX = 0;
            double centerY = 0;

            int n = polygon.Length;

            for (int i = 0; i < n; i++)
            {
                PointD current = polygon[i];
                PointD next = polygon[(i + 1) % n];

                double crossProduct = current.X * next.Y - next.X * current.Y;
                area += crossProduct;

                centerX += (current.X + next.X) * crossProduct;
                centerY += (current.Y + next.Y) * crossProduct;
            }

            area /= 2;

            if (Math.Abs(area) < 1e-10)
            {
                double sumX = 0, sumY = 0;
                for (int i = 0; i < n; i++)
                {
                    sumX += polygon[i].X;
                    sumY += polygon[i].Y;
                }
                return new PointD(sumX / n, sumY / n);
            }

            double factor = 1 / (6 * area);
            centerX *= factor;
            centerY *= factor;

            return new PointD(centerX, centerY);
        }
    }
}