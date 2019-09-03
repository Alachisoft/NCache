namespace Alachisoft.NGroups
{
    /// <summary>
    /// Defines the type of messages.
    /// </summary>
    class MsgType
    {
        public const byte TOKEN_SEEKING = 1;
        public const byte SEQUENCED = 2;
        public const byte SEQUENCE_LESS = 4;
    }
}