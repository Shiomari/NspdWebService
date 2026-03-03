using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NspdWebServer.Models;
using NspdWebService.Infrastructure.Enums;
using NspdWebService.Infrastructure.Structs;
using NspdWebService.Services;
using System.Text.Json;

namespace NspdWebServer.Pages
{
    /// <summary>
    /// Модель страницы карты для поиска и отображения объектов
    /// </summary>
    public class MapModel : PageModel
    {
        private readonly ILogger<MapModel> _logger;

        /// <summary>
        /// Модель представления страницы
        /// </summary>
        [BindProperty]
        public MapViewModel ViewModel { get; set; } = new();

        /// <summary>
        /// Инициализирует новый экземпляр MapModel
        /// </summary>
        /// <param name="logger">Логгер</param>
        public MapModel(ILogger<MapModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Обработчик GET-запроса для загрузки страницы
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            // Проверяем наличие параметров в URL
            if (Request.Query.ContainsKey("handler"))
            {
                var handler = Request.Query["handler"].ToString();

                if (handler.StartsWith("Search"))
                {
                    try
                    {
                        // Парсим параметры из URL
                        // Формат: Search?searchType?cadastralNumber
                        var parts = handler.Split('?');
                        if (parts.Length >= 3)
                        {
                            if (int.TryParse(parts[1], out int st))
                            {
                                ViewModel.SearchType = (SearchType)st;
                                ViewModel.CadastralNumber = Uri.UnescapeDataString(parts[2]);

                                // Выполняем поиск автоматически
                                return await OnPostSearchAsync();
                            }
                        }
                        else if (parts.Length == 2)
                        {
                            // Альтернативный формат: Search?cadastralNumber (с типом по умолчанию 1)
                            ViewModel.SearchType = SearchType.RealEstate;
                            ViewModel.CadastralNumber = Uri.UnescapeDataString(parts[1]);

                            // Выполняем поиск автоматически
                            return await OnPostSearchAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewModel.ErrorMessage = $"Ошибка обработки URL параметров: {ex.Message}";
                        _logger.LogError(ex, "Ошибка обработки URL параметров");
                    }
                }
            }

            // Обычная загрузка из TempData
            if (TempData.TryGetValue("CadastralNumber", out var cadNumber))
            {
                ViewModel.CadastralNumber = cadNumber?.ToString();
            }
            if (TempData.TryGetValue("SearchType", out var searchType))
            {
                if (int.TryParse(searchType?.ToString(), out var typeInt))
                {
                    ViewModel.SearchType = (SearchType)typeInt;
                }
            }
            if (TempData.TryGetValue("Zoom", out var zoom))
            {
                if (int.TryParse(zoom?.ToString(), out var zoomInt))
                {
                    ViewModel.Zoom = zoomInt;
                }
            }
            if (TempData.TryGetValue("FeatureInfo", out var featureInfoJson))
            {
                try
                {
                    ViewModel.FeatureInfo = JsonSerializer.Deserialize<FeatureInfo>(featureInfoJson.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка десериализации FeatureInfo");
                }
            }

            TempData.Keep();
            return Page();
        }

        /// <summary>
        /// Обработчик POST-запроса для поиска объектов
        /// </summary>
        /// <returns>Результат выполнения поиска</returns>
        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (!string.IsNullOrWhiteSpace(ViewModel.CadastralNumber))
            {
                ViewModel.CadastralNumber = ViewModel.CadastralNumber.Replace(" ", "").Trim();
            }

            TempData["CadastralNumber"] = ViewModel.CadastralNumber;
            TempData["SearchType"] = (int)ViewModel.SearchType;
            TempData["Zoom"] = ViewModel.Zoom;

            if (string.IsNullOrWhiteSpace(ViewModel.CadastralNumber))
            {
                ViewModel.ErrorMessage = "Введите кадастровый номер";
                TempData.Keep();
                return Page();
            }

            try
            {
                using var client = new Client();
                var feature = await client.GetInfo(ViewModel.CadastralNumber, ViewModel.SearchType);

                var coordinates = feature.Geometry?.Coordinates;
                if (coordinates == null || coordinates.Count == 0)
                {
                    ViewModel.ErrorMessage = "У объекта нет геометрии";
                    TempData.Keep();
                    return Page();
                }

                ViewModel.MultiPolygonCoordinates = coordinates;

                ViewModel.FeatureInfo = new FeatureInfo
                {
                    Id = feature.Id,
                    Type = feature.Type,
                    Properties = feature.Properties
                };

                var allPoints = new List<PointD>();
                foreach (var polygon in coordinates)
                {
                    foreach (var ring in polygon)
                    {
                        allPoints.AddRange(ring);
                    }
                }

                if (allPoints.Count > 0)
                {
                    var center = PointD.GetCentroid(allPoints.ToArray());
                    ViewModel.CenterX = center.X;
                    ViewModel.CenterY = center.Y;

                    ViewModel.Zoom = DetermineInitialZoom(allPoints);
                }
                else
                {
                    ViewModel.Zoom = 12;
                }

                TempData["Zoom"] = ViewModel.Zoom;

                var featureInfoJson = JsonSerializer.Serialize(ViewModel.FeatureInfo);
                TempData["FeatureInfo"] = featureInfoJson;

                var multiPolygonJson = JsonSerializer.Serialize(ViewModel.MultiPolygonCoordinates);
                HttpContext.Session.SetString("MultiPolygonCoordinates", multiPolygonJson);

                HttpContext.Session.SetString("HasSearchResults", "true");
                HttpContext.Session.SetString("CadastralNumber", ViewModel.CadastralNumber);
                HttpContext.Session.SetInt32("SearchType", (int)ViewModel.SearchType);
                HttpContext.Session.SetInt32("Zoom", ViewModel.Zoom);

                TempData.Keep();
                return Page();
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = $"Ошибка при поиске: {ex.Message}";
                TempData.Keep();
                _logger.LogError(ex, "Ошибка при поиске объекта");
                return Page();
            }
        }

        /// <summary>
        /// Определяет начальный уровень масштабирования на основе координат объекта
        /// </summary>
        /// <param name="coordinates">Координаты объекта</param>
        /// <returns>Уровень масштабирования</returns>
        private int DetermineInitialZoom(List<PointD> coordinates)
        {
            if (coordinates == null || coordinates.Count < 2) return 12;

            var minX = coordinates.Min(p => p.X);
            var maxX = coordinates.Max(p => p.X);
            var minY = coordinates.Min(p => p.Y);
            var maxY = coordinates.Max(p => p.Y);

            var width = maxX - minX;
            var height = maxY - minY;
            var maxSize = Math.Max(width, height);

            if (maxSize > 2000000) return 5;
            if (maxSize > 1400000) return 6;
            if (maxSize > 800000) return 7;
            if (maxSize > 400000) return 8;
            if (maxSize > 200000) return 9;
            if (maxSize > 100000) return 10;
            if (maxSize > 50000) return 11;
            if (maxSize > 20000) return 12;
            if (maxSize > 10000) return 13;
            if (maxSize > 5000) return 14;
            if (maxSize > 2000) return 15;
            if (maxSize > 1000) return 16;
            return 17;
        }

        /// <summary>
        /// Обработчик GET-запроса для загрузки тайлов карты
        /// </summary>
        /// <param name="x">Координата X тайла</param>
        /// <param name="y">Координата Y тайла</param>
        /// <param name="z">Уровень масштабирования</param>
        /// <returns>Изображение тайла</returns>
        public async Task<IActionResult> OnGetTile(int x, int y, int z)
        {
            try
            {
                _logger.LogInformation("Запрос тайла: z={z}, x={x}, y={y}", z, x, y);

                int maxTile = (1 << z) - 1;
                if (x < 0 || x > maxTile || y < 0 || y > maxTile || z < 0 || z > 18)
                {
                    _logger.LogWarning("Невалидные координаты тайла: z={z}, x={x}, y={y}", z, x, y);
                    return BadRequest("Invalid tile coordinates");
                }

                var sessionKey = $"tile_{z}_{x}_{y}";
                if (HttpContext.Session.TryGetValue(sessionKey, out var cachedTile))
                {
                    _logger.LogDebug("Тайл из кэша сессии: {sessionKey}", sessionKey);
                    SetCacheHeaders();
                    return File(cachedTile, "image/png");
                }

                _logger.LogDebug("Загрузка тайла из OSM: z={z}, x={x}, y={y}", z, x, y);

                using var client = new Client();

                try
                {
                    var tileData = await client.GetTile(x, y, z);

                    if (tileData == null || tileData.Length == 0)
                    {
                        _logger.LogWarning("Тайл пустой: z={z}, x={x}, y={y}", z, x, y);
                        return NotFound();
                    }

                    if (tileData.Length < 1024 * 50)
                    {
                        try
                        {
                            HttpContext.Session.Set(sessionKey, tileData);
                            _logger.LogDebug("Тайл закэширован в сессии: {sessionKey}, {size} байт",
                                sessionKey, tileData.Length);
                        }
                        catch (Exception cacheEx)
                        {
                            _logger.LogWarning(cacheEx, "Ошибка кэширования тайла {sessionKey}", sessionKey);
                        }
                    }

                    SetCacheHeaders();
                    return File(tileData, "image/png");
                }
                catch (Exception ex) when (ex.Message.Contains("Превышен лимит запросов"))
                {
                    _logger.LogWarning("Rate limit OSM для тайла z={z}, x={x}, y={y}: {Message}",
                        z, x, y, ex.Message);
                    return StatusCode(429, "Превышен лимит запросов к серверу OSM. Попробуйте позже.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при загрузке тайла z={z}, x={x}, y={y}", z, x, y);
                    return StatusCode(502, $"Ошибка загрузки тайла: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при загрузке тайла z={z}, x={x}, y={y}", z, x, y);
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        /// <summary>
        /// Устанавливает заголовки кэширования для тайлов
        /// </summary>
        private void SetCacheHeaders()
        {
            try
            {
                HttpContext.Response.Headers["Cache-Control"] = "public, max-age=604800";
                HttpContext.Response.Headers["Expires"] = DateTime.UtcNow.AddDays(7).ToString("R");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при установке заголовков кэширования");
            }
        }
    }
}