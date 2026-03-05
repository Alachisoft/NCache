namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Max access enum.
    /// </summary>
    public enum MaxAccess
    {
        /// <summary>
        /// This object is not accessible.
        /// </summary>
        NotAccessible,

        /// <summary>
        /// This object is accessible for notifications.
        /// </summary>
        AccessibleForNotify,

        /// <summary>
        /// This object is readonly.
        /// </summary>
        ReadOnly,

        /// <summary>
        /// This object is read-write.
        /// </summary>
        ReadWrite,

        /// <summary>
        /// This object is read-create (for tables).
        /// </summary>
        ReadCreate
    }

}
