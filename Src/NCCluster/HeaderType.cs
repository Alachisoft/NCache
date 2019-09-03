namespace Alachisoft.NGroups
{
    public class HeaderType
    {
        /// <summary>Toatal header </summary>
        public const byte TOTAL = 1;

        /// <summary>GMS header </summary>
        public const byte GMS = 2;

        /// <summary>NackAck header </summary>
        public const byte NACKACK = 3;

        /// <summary>TCP header </summary>
        public const byte TCP = 4;

        /// <summary>UDP header </summary>
        public const byte UDP = 5;

        /// <summary>TCPPING header </summary>
        public const byte  TCPPING = 6;

        /// <summary>QUEUE header </summary>
        public const byte QUEUE = 7;

        /// <summary>RequestCorrelator header </summary>
        public const byte REQUEST_COORELATOR = 8;

        /// <summary>MergeFast header </summary>
        public const byte MERGEFAST = 9;

        /// <summary>PING header </summary>
        public const byte PING = 10;

        /// <summary>Verfiy-suspect header </summary>
        public const byte VERIFY_SUSPECT = 11;

        /// <summary>Unicast header </summary>
        public const byte UNICAST = 12;

        /// <summary>FD_Sock header </summary>
        public const byte FD_SOCK = 13;

        /// <summary>STABLE header </summary>
        public const byte STABLE = 14;

        /// <summary>FRAG header </summary>
        public const byte FRAG = 15;

        /// <summary>Keep Alive header </summary>
        public const byte KEEP_ALIVE = 16;


    }
}