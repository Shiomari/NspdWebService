using NspdWebService.Infrastructure.Enums;
using NspdWebService.Infrastructure.Info;
using NspdWebService.Infrastructure.Structs;
using System.Text.Json.Serialization;

namespace NspdWebServer.Models
{
    /// <summary>
    /// Модель представления для страницы карты.
    /// </summary>
    public class MapViewModel
    {
        /// <summary>
        /// Кадастровый номер для поиска.
        /// </summary>
        public string CadastralNumber { get; set; } = "";

        /// <summary>
        /// Тип поиска.
        /// </summary>
        public SearchType SearchType { get; set; } = SearchType.RealEstate;

        /// <summary>
        /// Уровень масштабирования карты.
        /// </summary>
        public int Zoom { get; set; } = 15;

        /// <summary>
        /// Сообщение об ошибке.
        /// </summary>
        public string ErrorMessage { get; set; } = "";

        /// <summary>
        /// Координаты MultiPolygon объекта.
        /// </summary>
        public List<List<List<PointD>>>? MultiPolygonCoordinates { get; set; }

        /// <summary>
        /// Координата X центра объекта.
        /// </summary>
        public double? CenterX { get; set; }

        /// <summary>
        /// Координата Y центра объекта.
        /// </summary>
        public double? CenterY { get; set; }

        /// <summary>
        /// Информация об объекте.
        /// </summary>
        public FeatureInfo? FeatureInfo { get; set; }

        /// <summary>
        /// Динамические свойства объекта для отображения.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object>? FeaturePropertiesForDisplay
        {
            get
            {
                if (FeatureInfo?.Properties == null)
                    return null;

                var properties = new Dictionary<string, object>();

                // Добавляем базовые свойства
                AddPropertyIfNotEmpty(properties, "ID", FeatureInfo.Id.ToString());
                AddPropertyIfNotEmpty(properties, "Тип объекта", FeatureInfo.Type);

                var props = FeatureInfo.Properties;

                // Добавляем свойства Properties
                AddPropertyIfNotEmpty(properties, "Кадастровый номер", props.ExternalKey);
                AddPropertyIfNotEmpty(properties, "Код кадастрового округа", props.CadastralDistrictsCode, 0);
                AddPropertyIfNotEmpty(properties, "Категория (ID)", props.Category, 0);
                AddPropertyIfNotEmpty(properties, "Название категории", props.CategoryName);
                AddPropertyIfNotEmpty(properties, "Описание", props.Descr);
                AddPropertyIfNotEmpty(properties, "Метка", props.Label);
                AddPropertyIfNotEmpty(properties, "ID взаимодействия", props.InteractionId, 0);
                AddPropertyIfNotEmpty(properties, "Подкатегория", props.Subcategory, 0);
                AddPropertyIfNotEmpty(properties, "Оценка", props.Score);

                // Добавляем Options если они есть
                if (props.Options != null)
                {
                    var opts = props.Options;

                    // Основные свойства Options
                    AddPropertyIfNotEmpty(properties, "Площадь (м²)", opts.Area?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Кадастровый номер (опции)", opts.CadNum);
                    AddPropertyIfNotEmpty(properties, "Адрес", opts.ReadableAddress);
                    AddPropertyIfNotEmpty(properties, "Статус", opts.Status);
                    AddPropertyIfNotEmpty(properties, "Кадастровая стоимость (руб.)", opts.CostValue?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Индекс стоимости", opts.CostIndex?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Уточненная площадь (м²)", opts.SpecifiedArea?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Заявленная площадь (м²)", opts.DeclaredArea > 0 ? opts.DeclaredArea?.ToString("N2") : null);
                    AddPropertyIfNotEmpty(properties, "Кадастровый квартал", opts.QuarterCadNumber);
                    AddPropertyIfNotEmpty(properties, "Вид права", opts.RightType);
                    AddPropertyIfNotEmpty(properties, "Тип собственности", opts.OwnershipType);
                    AddPropertyIfNotEmpty(properties, "Тип записи о земле", opts.LandRecordType);
                    AddPropertyIfNotEmpty(properties, "Подтип записи о земле", opts.LandRecordSubtype);
                    AddPropertyIfNotEmpty(properties, "Тип категории записи о земле", opts.LandRecordCategoryType);
                    AddPropertyIfNotEmpty(properties, "Разрешенное использование по документу", opts.PermittedUseEstablishedByDocument);
                    AddPropertyIfNotEmpty(properties, "Ранее опубликовано", opts.PreviouslyPosted);
                    AddPropertyIfNotEmpty(properties, "Причина определения", opts.DeterminationCause);

                    // Даты
                    AddPropertyIfNotEmpty(properties, "Дата подачи заявления о стоимости", opts.CostApplicationDate);
                    AddPropertyIfNotEmpty(properties, "Дата утверждения стоимости", opts.CostApprovementDate);
                    AddPropertyIfNotEmpty(properties, "Дата определения стоимости", opts.CostDeterminationDate);
                    AddPropertyIfNotEmpty(properties, "Дата регистрации стоимости", opts.CostRegistrationDate);
                    AddPropertyIfNotEmpty(properties, "Дата регистрации записи о земле", opts.LandRecordRegDate);

                    // Счетчики
                    AddPropertyIfNotEmpty(properties, "Кол-во земельных участков", opts.CntLand);
                    AddPropertyIfNotEmpty(properties, "Кол-во зем. уч. с геометрией", opts.CntLandGeom);
                    AddPropertyIfNotEmpty(properties, "Кол-во зем. уч. без геометрии", opts.CntLandNotGeom);
                    AddPropertyIfNotEmpty(properties, "Кол-во ОКС", opts.CntOks);
                    AddPropertyIfNotEmpty(properties, "Кол-во ОКС с геометрией", opts.CntOksGeom);
                    AddPropertyIfNotEmpty(properties, "Кол-во ОКС без геометрии", opts.CntOksNotGeom);
                    AddPropertyIfNotEmpty(properties, "Кол-во КК", opts.CntKk);
                    AddPropertyIfNotEmpty(properties, "Кол-во ПИК", opts.CntPik);
                    AddPropertyIfNotEmpty(properties, "Кол-во ЕНК", opts.CntEnk);

                    // Суммы и площади
                    AddPropertyIfNotEmpty(properties, "Общая стоимость с геометрией (руб.)",
                        opts.CostValueTotalGeom > 0 ? opts.CostValueTotalGeom?.ToString("N2") : null);
                    AddPropertyIfNotEmpty(properties, "Суммарная площадь земель (м²)",
                        opts.SumLandArea > 0 ? opts.SumLandArea?.ToString("N2") : null);
                    AddPropertyIfNotEmpty(properties, "Суммарная площадь зем. уч. с геом. (м²)",
                        opts.SumLandGeomArea > 0 ? opts.SumLandGeomArea?.ToString("N2") : null);

                    // Другие поля
                    AddPropertyIfNotEmpty(properties, "Дата создания", opts.DateCr);
                    AddPropertyIfNotEmpty(properties, "Дата изменения", opts.DateCh);
                    AddPropertyIfNotEmpty(properties, "GUID", opts.Guid);
                    AddPropertyIfNotEmpty(properties, "ID (опции)", opts.Id);
                    AddPropertyIfNotEmpty(properties, "Актуально", opts.IsActual.HasValue ? (opts.IsActual.Value ? "Да" : "Нет") : null);
                    AddPropertyIfNotEmpty(properties, "Условный", opts.IsConditional.HasValue ? (opts.IsConditional.Value ? "Да" : "Нет") : null);
                    AddPropertyIfNotEmpty(properties, "ID КР", opts.KrId);
                    AddPropertyIfNotEmpty(properties, "Примечание", opts.Note);
                    AddPropertyIfNotEmpty(properties, "Вид объекта", opts.ObjKind);
                    AddPropertyIfNotEmpty(properties, "Значение вида объекта", opts.ObjKindValue);
                    AddPropertyIfNotEmpty(properties, "Метка объекта", opts.ObjLabel);
                    AddPropertyIfNotEmpty(properties, "Real SRID", opts.RealSrid);
                    AddPropertyIfNotEmpty(properties, "Регистрационный код", opts.RegCode);
                    AddPropertyIfNotEmpty(properties, "Допуск", opts.Tolerance);
                    AddPropertyIfNotEmpty(properties, "UA ID", opts.UaId);
                    AddPropertyIfNotEmpty(properties, "KO ID", opts.KoId);
                    AddPropertyIfNotEmpty(properties, "Название", opts.Name);

                    // Обработка полей для зданий
                    if (!string.IsNullOrEmpty(opts.BuildingName))
                        AddPropertyIfNotEmpty(properties, "Название здания", opts.BuildingName);

                    AddPropertyIfNotEmpty(properties, "Площадь здания (м²)", opts.BuildRecordArea?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Дата регистрации здания", opts.BuildRecordRegistrationDate);
                    AddPropertyIfNotEmpty(properties, "Тип здания", opts.BuildRecordTypeValue);
                    AddPropertyIfNotEmpty(properties, "Статус объекта", opts.CommonDataStatus);
                    AddPropertyIfNotEmpty(properties, "Этажность", opts.Floors);
                    AddPropertyIfNotEmpty(properties, "Материал стен", opts.Materials);
                    AddPropertyIfNotEmpty(properties, "Назначение", opts.Purpose);
                    AddPropertyIfNotEmpty(properties, "Разрешенное использование", opts.PermittedUseName);
                    AddPropertyIfNotEmpty(properties, "Год постройки", opts.YearBuilt);
                    AddPropertyIfNotEmpty(properties, "Год ввода в эксплуатацию", opts.YearCommisioning);
                    AddPropertyIfNotEmpty(properties, "Подземные этажи", opts.UndergroundFloors);

                    // Для земельных участков
                    AddPropertyIfNotEmpty(properties, "Площадь по документам (м²)", opts.LandRecordArea?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Заявленная площадь (м²)", opts.LandRecordAreaDeclaration?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Уточненная площадь (м²)", opts.LandRecordAreaVerified?.ToString("N2"));
                    AddPropertyIfNotEmpty(properties, "Дата регистрации", opts.RegistrationDate);
                    AddPropertyIfNotEmpty(properties, "Подтип", opts.Subtype);
                }

                // Добавляем SystemInfo если они есть
                if (props.SystemInfo != null)
                {
                    var sys = props.SystemInfo;

                    if (sys.Inserted != System.DateTime.MinValue)
                        properties["Дата создания (системная)"] = sys.Inserted.ToString("dd.MM.yyyy HH:mm");

                    AddPropertyIfNotEmpty(properties, "Создал", sys.InsertedBy);

                    if (sys.Updated != System.DateTime.MinValue)
                        properties["Дата обновления (системная)"] = sys.Updated.ToString("dd.MM.yyyy HH:mm");

                    AddPropertyIfNotEmpty(properties, "Обновил", sys.UpdatedBy);
                }

                return properties;
            }
        }

        /// <summary>
        /// Координаты полигона (для обратной совместимости).
        /// </summary>
        [JsonIgnore]
        public List<PointD>? PolygonCoordinates
        {
            get
            {
                if (MultiPolygonCoordinates == null || MultiPolygonCoordinates.Count == 0)
                    return null;

                if (MultiPolygonCoordinates[0].Count > 0)
                    return MultiPolygonCoordinates[0][0];

                return null;
            }
        }

        /// <summary>
        /// Опции для выбора типа поиска.
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, string> SearchTypeOptions => new()
        {
            { 1, "Объекты недвижимости" },
            { 2, "Кадастровое деление" },
            { 4, "Административно-территориальные единицы" },
            { 5, "ЗОУИТ (Зоны и территории)" },
            { 7, "Территориальные зоны" }
        };

        // Вспомогательные методы для добавления свойств
        private void AddPropertyIfNotEmpty(Dictionary<string, object> dict, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                dict[key] = value;
        }

        private void AddPropertyIfNotEmpty(Dictionary<string, object> dict, string key, int value, int defaultValue = 0)
        {
            if (value != defaultValue)
                dict[key] = value;
        }

        private void AddPropertyIfNotEmpty(Dictionary<string, object> dict, string key, int? value)
        {
            if (value.HasValue)
                dict[key] = value.Value;
        }

        //private void AddPropertyIfNotEmpty(Dictionary<string, object> dict, string key, double? value)
        //{
        //    if (value.HasValue)
        //        dict[key] = value.Value;
        //}

        //private void AddPropertyIfNotEmpty(Dictionary<string, object> dict, string key, bool? value)
        //{
        //    if (value.HasValue)
        //        dict[key] = value.Value ? "Да" : "Нет";
        //}
    }

    /// <summary>
    /// Модель для отображения информации об объекте.
    /// </summary>
    public class FeatureInfo
    {
        /// <summary>
        /// Идентификатор объекта.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Тип объекта.
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// Свойства объекта.
        /// </summary>
        public Properties Properties { get; set; } = new();
    }
}