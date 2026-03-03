using NspdWebService.Infrastructure.Info;
using System.Runtime.Caching;

namespace NspdWebService.Infrastructure.Cache
{
    /// <summary>
    /// Кэш объектов Feature в памяти с использованием MemoryCache.
    /// </summary>
    public class MemoryFeatureCache
    {
        private readonly MemoryCache _cache;
        private readonly CacheItemPolicy _policy;

        /// <summary>
        /// Инициализирует новый экземпляр кэша с политикой скользящего срока действия.
        /// </summary>
        public MemoryFeatureCache()
        {
            _cache = MemoryCache.Default;

            _policy = new CacheItemPolicy
            {
                Priority = CacheItemPriority.Default,
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };
        }

        /// <summary>
        /// Добавляет объект Feature в кэш.
        /// </summary>
        /// <param name="key">Ключ для кэширования.</param>
        /// <param name="feature">Объект Feature для кэширования.</param>
        public void AddFeature(string key, Feature feature)
        {
            _cache.Set(key, feature, _policy);
        }

        /// <summary>
        /// Получает объект Feature из кэша по ключу.
        /// </summary>
        /// <param name="key">Ключ объекта.</param>
        /// <returns>Объект Feature или null если не найден.</returns>
        public Feature GetFeature(string key)
        {
            return _cache.Get(key) as Feature;
        }
    }
}