namespace Alachisoft.NCache.EntityFrameworkCore
{
    /// <summary>
    /// Specifies the expiration type of the result set.
    /// </summary>
    public enum ExpirationType
    {
        /// <summary>
        /// Specifies that the result set will be stored with absolute expiration.
        /// </summary>
        Absolute,

        /// <summary>
        /// Specifies that the result set will be stored with sliding expiration.
        /// </summary>
        Sliding
    }

    /// <summary>
    /// Specifies how to store result set in cache
    /// </summary>
    public enum StoreAs
    {
        /// <summary>
        /// Specifies that the result set will be stored as seperate entities.
        /// </summary>
        SeperateEntities,

        /// <summary>
        /// Specifies that the result set will be stored as a single collection.
        /// </summary>
        Collection
    }

    /// <summary>
    /// This enum is used to created database dependency to invalidate data from the cache.
    /// It should be configured for data invalidation to work properly.
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// No database dependency will be created
        /// </summary>
        Other,

        /// <summary>
        /// Sql Server (yukon and above) database dependency will be created.
        /// </summary>
        SqlServer,

        /// <summary>
        /// Oracle (10i Release 2 and above) dependency will be created.
        /// </summary>
        Oracle
    }

    internal enum CachingMethod
    {
        FromCache,
        LoadIntoCache
    }
}
