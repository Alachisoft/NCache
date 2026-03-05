namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Status enum.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// This object can be used currently.
        /// </summary>
        Current,

        /// <summary>
        /// This object is deprecated.
        /// </summary>
        Deprecated,

        /// <summary>
        /// This object is obsolete.
        /// </summary>
        Obsolete,

        /// <summary>
        /// This object is mandatory.
        /// </summary>
        Mandatory,

        /// <summary>
        /// This object is optional.
        /// </summary>
        Optional
    }

}
