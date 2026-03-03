using System.Text.Json.Serialization;
using System.Text.Json;
using NspdWebService.Infrastructure.Structs;

namespace NspdWebService.Infrastructure.Info
{
    /// <summary>
    /// Основная модель ответа API геопортала НСПД.
    /// </summary>
    public class SubjectInfo
    {
        [JsonPropertyName("data")]
        public Data? Data { get; set; }

        [JsonPropertyName("meta")]
        public List<MetaItem>? Meta { get; set; }
    }

    /// <summary>
    /// Данные ответа API.
    /// </summary>
    public class Data
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("features")]
        public List<Feature>? Features { get; set; }
    }

    /// <summary>
    /// Геометрический объект с свойствами.
    /// </summary>
    public class Feature
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("geometry")]
        public Geometry? Geometry { get; set; }

        [JsonPropertyName("properties")]
        public Properties? Properties { get; set; }
    }

    /// <summary>
    /// Геометрическое описание объекта.
    /// </summary>
    public class Geometry
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("coordinates")]
        [JsonConverter(typeof(CoordinatesConverter))]
        public List<List<List<PointD>>>? Coordinates { get; set; }

        [JsonPropertyName("crs")]
        public Crs? Crs { get; set; }
    }

    /// <summary>
    /// Система координат.
    /// </summary>
    public class Crs
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("properties")]
        public CrsProperties? Properties { get; set; }
    }

    /// <summary>
    /// Свойства системы координат.
    /// </summary>
    public class CrsProperties
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Конвертер для преобразования координат MultiPolygon/Polygon.
    /// </summary>
    public class CoordinatesConverter : JsonConverter<List<List<List<PointD>>>>
    {
        public override List<List<List<PointD>>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var polygons = new List<List<List<PointD>>>();

            if (reader.TokenType != JsonTokenType.StartArray)
                return polygons;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Проверяем, является ли geometry Point или Polygon
            if (root.ValueKind == JsonValueKind.Array)
            {
                ProcessCoordinates(root, polygons);
            }
            else if (root.ValueKind == JsonValueKind.String)
            {
                // Это точка, возвращаем пустой список полигонов
                return polygons;
            }

            return polygons;
        }

        private void ProcessCoordinates(JsonElement element, List<List<List<PointD>>> polygons)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                int depth = GetArrayDepth(element);

                if (depth == 4)
                {
                    // MultiPolygon или Polygon в формате FeatureCollection
                    foreach (var polygon in element.EnumerateArray())
                    {
                        var rings = new List<List<PointD>>();

                        foreach (var ring in polygon.EnumerateArray())
                        {
                            var points = new List<PointD>();

                            foreach (var point in ring.EnumerateArray())
                            {
                                if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() >= 2)
                                {
                                    double x = point[0].GetDouble();
                                    double y = point[1].GetDouble();
                                    points.Add(new PointD(x, y));
                                }
                            }

                            if (points.Count > 0)
                            {
                                rings.Add(points);
                            }
                        }

                        if (rings.Count > 0)
                        {
                            polygons.Add(rings);
                        }
                    }
                }
                else if (depth == 3)
                {
                    // Polygon с одним кольцом
                    var rings = new List<List<PointD>>();

                    foreach (var ring in element.EnumerateArray())
                    {
                        var points = new List<PointD>();

                        foreach (var point in ring.EnumerateArray())
                        {
                            if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() >= 2)
                            {
                                double x = point[0].GetDouble();
                                double y = point[1].GetDouble();
                                points.Add(new PointD(x, y));
                            }
                        }

                        if (points.Count > 0)
                        {
                            rings.Add(points);
                        }
                    }

                    if (rings.Count > 0)
                    {
                        polygons.Add(rings);
                    }
                }
                else if (depth == 2)
                {
                    // Только точки (линия или упрощенный полигон)
                    var points = new List<PointD>();

                    foreach (var point in element.EnumerateArray())
                    {
                        if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() >= 2)
                        {
                            double x = point[0].GetDouble();
                            double y = point[1].GetDouble();
                            points.Add(new PointD(x, y));
                        }
                    }

                    if (points.Count > 0)
                    {
                        var rings = new List<List<PointD>> { points };
                        polygons.Add(rings);
                    }
                }
                else if (depth == 1)
                {
                    // Одна точка (Point geometry)
                    var points = new List<PointD>();

                    if (element.GetArrayLength() >= 2)
                    {
                        double x = element[0].GetDouble();
                        double y = element[1].GetDouble();
                        points.Add(new PointD(x, y));

                        var rings = new List<List<PointD>> { points };
                        polygons.Add(rings);
                    }
                }
            }
        }

        private int GetArrayDepth(JsonElement element)
        {
            int depth = 0;
            var current = element;

            while (current.ValueKind == JsonValueKind.Array)
            {
                depth++;
                if (current.GetArrayLength() > 0)
                    current = current[0];
                else
                    break;
            }

            return depth;
        }

        public override void Write(Utf8JsonWriter writer, List<List<List<PointD>>> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var polygon in value)
            {
                writer.WriteStartArray();

                foreach (var ring in polygon)
                {
                    writer.WriteStartArray();

                    foreach (var point in ring)
                    {
                        writer.WriteStartArray();
                        writer.WriteNumberValue(point.X);
                        writer.WriteNumberValue(point.Y);
                        writer.WriteEndArray();
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Свойства геометрического объекта.
    /// </summary>
    public class Properties
    {
        [JsonPropertyName("cadastralDistrictsCode")]
        public int CadastralDistrictsCode { get; set; }

        [JsonPropertyName("category")]
        public int Category { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("descr")]
        public string? Descr { get; set; }

        [JsonPropertyName("externalKey")]
        public string? ExternalKey { get; set; }

        [JsonPropertyName("interactionId")]
        public int InteractionId { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("options")]
        public Options? Options { get; set; }

        [JsonPropertyName("subcategory")]
        public int Subcategory { get; set; }

        [JsonPropertyName("systemInfo")]
        public SystemInfo? SystemInfo { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }
    }

    /// <summary>
    /// Дополнительные опции объекта.
    /// </summary>
    public class Options
    {
        [JsonPropertyName("area")]
        public double? Area { get; set; }

        [JsonPropertyName("cad_num")]
        public string? CadNum { get; set; }

        [JsonPropertyName("cost_application_date")]
        public string? CostApplicationDate { get; set; }

        [JsonPropertyName("cost_approvement_date")]
        public string? CostApprovementDate { get; set; }

        [JsonPropertyName("cost_determination_date")]
        public string? CostDeterminationDate { get; set; }

        [JsonPropertyName("cost_index")]
        public double? CostIndex { get; set; }

        [JsonPropertyName("cost_registration_date")]
        public string? CostRegistrationDate { get; set; }

        [JsonPropertyName("cost_value")]
        public double? CostValue { get; set; }

        [JsonPropertyName("declared_area")]
        public double? DeclaredArea { get; set; }

        [JsonPropertyName("determination_couse")]
        public string? DeterminationCause { get; set; }

        [JsonPropertyName("land_record_category_type")]
        public string? LandRecordCategoryType { get; set; }

        [JsonPropertyName("land_record_reg_date")]
        public string? LandRecordRegDate { get; set; }

        [JsonPropertyName("land_record_subtype")]
        public string? LandRecordSubtype { get; set; }

        [JsonPropertyName("land_record_type")]
        public string? LandRecordType { get; set; }

        [JsonPropertyName("ownership_type")]
        public string? OwnershipType { get; set; }

        [JsonPropertyName("permitted_use_established_by_document")]
        public string? PermittedUseEstablishedByDocument { get; set; }

        [JsonPropertyName("previously_posted")]
        public string? PreviouslyPosted { get; set; }

        [JsonPropertyName("quarter_cad_number")]
        public string? QuarterCadNumber { get; set; }

        [JsonPropertyName("readable_address")]
        public string? ReadableAddress { get; set; }

        [JsonPropertyName("right_type")]
        public string? RightType { get; set; }

        [JsonPropertyName("specified_area")]
        public double? SpecifiedArea { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("cnt_land")]
        public int? CntLand { get; set; }

        [JsonPropertyName("cnt_land_geom")]
        public int? CntLandGeom { get; set; }

        [JsonPropertyName("cnt_land_not_geom")]
        public int? CntLandNotGeom { get; set; }

        [JsonPropertyName("cnt_oks")]
        public int? CntOks { get; set; }

        [JsonPropertyName("cnt_oks_geom")]
        public int? CntOksGeom { get; set; }

        [JsonPropertyName("cnt_oks_not_geom")]
        public int? CntOksNotGeom { get; set; }

        [JsonPropertyName("cost_value_total_geom")]
        public double? CostValueTotalGeom { get; set; }

        [JsonPropertyName("date_cr")]
        public string? DateCr { get; set; }

        [JsonPropertyName("guid")]
        public string? Guid { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("is_actual")]
        public bool? IsActual { get; set; }

        [JsonPropertyName("is_conditional")]
        public bool? IsConditional { get; set; }

        [JsonPropertyName("kr_id")]
        public int? KrId { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("obj_kind")]
        public string? ObjKind { get; set; }

        [JsonPropertyName("obj_kind_value")]
        public string? ObjKindValue { get; set; }

        [JsonPropertyName("obj_label")]
        public string? ObjLabel { get; set; }

        [JsonPropertyName("real_srid")]
        public int? RealSrid { get; set; }

        [JsonPropertyName("reg_code")]
        public string? RegCode { get; set; }

        [JsonPropertyName("sum_land_area")]
        public double? SumLandArea { get; set; }

        [JsonPropertyName("sum_land_geom_area")]
        public double? SumLandGeomArea { get; set; }

        [JsonPropertyName("tolerance")]
        public string? Tolerance { get; set; }

        [JsonPropertyName("ua_id")]
        public int? UaId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("ko_id")]
        public int? KoId { get; set; }

        [JsonPropertyName("cnt_kk")]
        public int? CntKk { get; set; }

        [JsonPropertyName("cnt_pik")]
        public int? CntPik { get; set; }

        [JsonPropertyName("cnt_enk")]
        public int? CntEnk { get; set; }

        [JsonPropertyName("date_ch")]
        public string? DateCh { get; set; }

        [JsonPropertyName("build_record_area")]
        public double? BuildRecordArea { get; set; }

        [JsonPropertyName("build_record_registration_date")]
        public string? BuildRecordRegistrationDate { get; set; }

        [JsonPropertyName("build_record_type_value")]
        public string? BuildRecordTypeValue { get; set; }

        [JsonPropertyName("building_name")]
        public string? BuildingName { get; set; }

        [JsonPropertyName("common_data_status")]
        public string? CommonDataStatus { get; set; }

        [JsonPropertyName("cultural_heritage_object")]
        public object? CulturalHeritageObject { get; set; }

        [JsonPropertyName("cultural_heritage_val")]
        public string? CulturalHeritageVal { get; set; }

        [JsonPropertyName("facility_cad_number")]
        public string? FacilityCadNumber { get; set; }

        [JsonPropertyName("floors")]
        public string? Floors { get; set; }

        [JsonPropertyName("intersected_cad_numbers")]
        public object? IntersectedCadNumbers { get; set; }

        [JsonPropertyName("materials")]
        public string? Materials { get; set; }

        [JsonPropertyName("permitted_use_name")]
        public string? PermittedUseName { get; set; }

        [JsonPropertyName("purpose")]
        public string? Purpose { get; set; }

        [JsonPropertyName("registration_date")]
        public string? RegistrationDate { get; set; }

        [JsonPropertyName("underground_floors")]
        public string? UndergroundFloors { get; set; }

        [JsonPropertyName("united_cad_number")]
        public object? UnitedCadNumber { get; set; }

        [JsonPropertyName("united_cad_numbers")]
        public string? UnitedCadNumbers { get; set; }

        [JsonPropertyName("year_built")]
        public string? YearBuilt { get; set; }

        [JsonPropertyName("year_commisioning")]
        public string? YearCommisioning { get; set; }

        [JsonPropertyName("land_record_area")]
        public double? LandRecordArea { get; set; }

        [JsonPropertyName("land_record_area_declaration")]
        public double? LandRecordAreaDeclaration { get; set; }

        [JsonPropertyName("land_record_area_verified")]
        public double? LandRecordAreaVerified { get; set; }

        [JsonPropertyName("subtype")]
        public string? Subtype { get; set; }
    }

    /// <summary>
    /// Системная информация об объекте.
    /// </summary>
    public class SystemInfo
    {
        [JsonPropertyName("inserted")]
        public DateTime Inserted { get; set; }

        [JsonPropertyName("insertedBy")]
        public string? InsertedBy { get; set; }

        [JsonPropertyName("updated")]
        public DateTime Updated { get; set; }

        [JsonPropertyName("updatedBy")]
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Мета-информация ответа API.
    /// </summary>
    public class MetaItem
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("categoryId")]
        public int CategoryId { get; set; }
    }
}