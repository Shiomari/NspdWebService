namespace NspdWebService.Infrastructure.Enums
{
    /// <summary>
    /// Типы поиска в геопортале НСПД.
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        /// Поиск объектов недвижимости.
        /// </summary>
        RealEstate = 1,

        /// <summary>
        /// Поиск по кадастровому делению.
        /// </summary>
        CadastralDivision = 2,

        /// <summary>
        /// Поиск административно-территориальных единиц.
        /// </summary>
        AdministrativeTerritorialUnits = 4,

        /// <summary>
        /// Поиск зон и территорий (ЗОУИТ).
        /// </summary>
        ZOUIT = 5,

        /// <summary>
        /// Поиск территориальных зон.
        /// </summary>
        TerritorialZones = 7
    }
}