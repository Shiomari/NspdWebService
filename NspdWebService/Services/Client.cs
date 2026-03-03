using NspdWebService.Infrastructure.Cache;
using NspdWebService.Infrastructure.Enums;
using NspdWebService.Infrastructure.Info;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NspdWebService.Services
{
    /// <summary>
    /// HTTP-клиент для взаимодействия с API НСПД и сервером тайлов OSM.
    /// </summary>
    public class Client : IDisposable
    {
        /// <summary>
        /// Кэш данных НСПД.
        /// </summary>
        internal static MemoryFeatureCache _featureCache = new MemoryFeatureCache();

        /// <summary>
        /// Клиент для запросов в OSM.
        /// </summary>
        private static readonly HttpClient _tileClient;

        static Client()
        {
            _tileClient = new HttpClient(new HttpClientHandler
            {
                UseCookies = false,
                MaxConnectionsPerServer = 20
            });

            // выставите свои данные, требуемые OSM (+Ссылка на сайт; Почта для связи)
            _tileClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "NspdWebServer/0.0.1 (+https://vk.com/shioma; contact: ha4er@mail.ru)");

            _tileClient.Timeout = TimeSpan.FromSeconds(120);
        }



        /// <summary>
        /// Клиент для запросов в НСПД.
        /// </summary>
        HttpClient _nspdClient;

        /// <summary>
        /// Инициализирует новый экземпляр клиента
        /// </summary>
        public Client()
            : base()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
            };

            _nspdClient = new HttpClient(handler);
            ConfigureDefaultHeaders();
        }

        private void ConfigureDefaultHeaders()
        {
            _nspdClient.DefaultRequestHeaders.Clear();
            _nspdClient.DefaultRequestHeaders.Add("accept", "*/*");
            _nspdClient.DefaultRequestHeaders.Add("accept-language", "ru,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
            _nspdClient.DefaultRequestHeaders.Add("priority", "u=1, i");
            _nspdClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not(A:Brand\";v=\"8\", \"Chromium\";v=\"144\", \"Microsoft Edge\";v=\"144\"");
            _nspdClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            _nspdClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        }

        /// <summary>
        /// Получает информацию об объекте по кадастровому номеру и типу поиска.
        /// </summary>
        /// <param name="number">Кадастровый номер или идентификатор.</param>
        /// <param name="searchType">Тип поиска.</param>
        /// <returns>Объект Feature с информацией об объекте.</returns>
        public async Task<Feature> GetInfo(string number, SearchType searchType)
        {
            var featureCode = (int)searchType + "_" + number;

            // пробуем получить данные из кэша
            var feature = _featureCache.GetFeature(featureCode);
            if (feature != null)
            {
                return feature;
            }

            // запрашиваем данные об объекте из НСПД
            SubjectInfo info = await GetSubjectInfo(number, searchType);

            // если данные нашли
            if (info?.Data?.Features != null)
            {
                // проходим по всем данным
                foreach (Feature f in info.Data.Features)
                {
                    // сохраняем данные в кэш
                    var code = (int)searchType + "_" + f.Properties.ExternalKey;
                    _featureCache.AddFeature(code, f);
                }

                // получаем данные из кэша
                feature = _featureCache.GetFeature(featureCode);
                if (feature != null)
                {
                    return feature;
                }
            }

            throw new Exception("Не найден субъект по указанным данным поиска.");
        }

        /// <summary>
        /// Получает данные об объекте из НСПД.
        /// </summary>
        /// <param name="number">Кадастровый номер или идентификатор.</param>
        /// <param name="searchType">Тип поиска.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<SubjectInfo> GetSubjectInfo(string number, SearchType searchType)
        {
            string pattern = "";
            // определяем паттерны поиска
            switch (searchType)
            {
                case SearchType.RealEstate:
                    pattern = @"^\d{2}:\d{2}:\d{7}:\d{1,4}$";
                    break;

                case SearchType.CadastralDivision:
                    pattern = @"^\d{2}(?::\d{2}(?::\d{7})?)?$";
                    break;

                case SearchType.AdministrativeTerritorialUnits:
                    pattern = @"^\d{2}:\d{2}-\d+\.\d+$";
                    break;

                case SearchType.ZOUIT:
                    pattern = @"^\d{2}:\d{2}-\d+\.\d+$";
                    break;

                case SearchType.TerritorialZones:
                    pattern = @"^\d{2}:\d{2}-\d+\.\d+$";
                    break;
            }

            // проверяем соответствие паттерну
            if (!Regex.IsMatch(number, pattern))
            {
                string message = "Указанный номер не соответствует формату для типа поиска ";
                switch (searchType)
                {
                    case SearchType.RealEstate:
                        message += "'Объекты недвижимости':\n" +
                                  "Формат: РР:ОО:РРРРРРР:УУУУ\n" +
                                  "Пример: 23:19:0202007:18\n" +
                                  "Где:\n" +
                                  "• РР - регион (2 цифры)\n" +
                                  "• ОО - округ (2 цифры)\n" +
                                  "• РРРРРРР - район (7 цифр)\n" +
                                  "• УУУУ - участок (от 1 до 4 цифр)";
                        break;
                    case SearchType.CadastralDivision:
                        message += "'Кадастровое деление':\n" +
                                  "Возможные форматы:\n" +
                                  "1. РР (только регион) - Пример: 23\n" +
                                  "2. РР:ОО (регион:округ) - Пример: 23:19\n" +
                                  "3. РР:ОО:РРРРРРР (регион:округ:район) - Пример: 23:19:0202007\n" +
                                  "Где:\n" +
                                  "• РР - регион (2 цифры)\n" +
                                  "• ОО - округ (2 цифры)\n" +
                                  "• РРРРРРР - район (7 цифр)";
                        break;
                    case SearchType.AdministrativeTerritorialUnits:
                        message += "'Административно-территориальные единицы':\n" +
                                  "Формат: РР:КК-Н.П\n" +
                                  "Пример: 23:19-4.18\n" +
                                  "Где:\n" +
                                  "• РР - регион (2 цифры)\n" +
                                  "• КК - код (2 цифры)\n" +
                                  "• Н - номер (одна или более цифр)\n" +
                                  "• П - подномер (одна или более цифр)";
                        break;
                    case SearchType.ZOUIT:
                        message += "'ЗОУИТ (Зоны и территории)':\n" +
                                  "Формат: РР:КК-Н.П\n" +
                                  "Пример: 61:00-6.569\n" +
                                  "Где:\n" +
                                  "• РР - регион (2 цифры)\n" +
                                  "• КК - код (2 цифры)\n" +
                                  "• Н - номер (одна или более цифр)\n" +
                                  "• П - подномер (одна или более цифр)";
                        break;
                    case SearchType.TerritorialZones:
                        message += "'Территориальные зоны':\n" +
                                  "Формат: РР:КК-Н.П\n" +
                                  "Пример: 09:07-7.108\n" +
                                  "Где:\n" +
                                  "• РР - регион (2 цифры)\n" +
                                  "• КК - код (2 цифры)\n" +
                                  "• Н - номер (одна или более цифр)\n" +
                                  "• П - подномер (одна или более цифр)";
                        break;
                }
                throw new Exception(message);
            }

            // строка GET запроса из НПСД
            string targetUrl = $"https://nspd.gov.ru/api/geoportal/v2/search/geoportal?thematicSearchId={(int)searchType}&query={number}";

            var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            // обязательна
            request.Headers.Referrer = new Uri("https://nspd.gov.ru/map?thematic=PKK");

            // отправляем запрос
            HttpResponseMessage response = await _nspdClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception("Не найден субъект по указанным данным поиска или сервер НСПД не доступен.");
            }

            // проверяем ошибки запроса
            response.EnsureSuccessStatusCode();

            // получаем результат
            var result = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new CoordinatesConverter() }
            };

            // преобразуем в данные
            return JsonSerializer.Deserialize<SubjectInfo>(result, options);
        }

        /// <summary>
        /// Загружает тайл карты с сервера OSM.
        /// </summary>
        /// <param name="tileX">Координата X тайла</param>
        /// <param name="tileY">Координата Y тайла</param>
        /// <param name="zoom">Уровень масштабирования</param>
        /// <returns>Байтовый массив с изображением тайла</returns>
        public async Task<byte[]> GetTile(int tileX, int tileY, int zoom)
        {
            int maxTile = (1 << zoom) - 1;
            if (tileX < 0 || tileX > maxTile || tileY < 0 || tileY > maxTile)
            {
                throw new ArgumentException($"Invalid tile coordinates: {zoom}/{tileX}/{tileY}");
            }

            string url = $"https://tile.openstreetmap.org/{zoom}/{tileX}/{tileY}.png";

            try
            {
                var response = await _tileClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception($"Тайл не найден: {zoom}/{tileX}/{tileY}");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    throw new Exception("Превышен лимит запросов к серверу тайлов OSM.");
                }
                else
                {
                    throw new Exception($"Ошибка загрузки тайла: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Ошибка сети при загрузке тайла: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Таймаут при загрузке тайла");
            }
        }

        /// <summary>
        /// Освобождает ресурсы НСПД клиента.
        /// </summary>
        public void Dispose()
        {
            _nspdClient.Dispose();
        }
    }
}