using System;

namespace Memcached.ClientLibrary
{
	/// <summary>
	/// Tool to calculate and add CRC codes to a string
	/// Code found posted on CodeProject.com:
	/// http://www.codeproject.com/csharp/marcelcrcencoding.asp
	/// 
    /// ***************************************************************************
    /// Copyright (c) 2003 Thoraxcentrum, Erasmus MC, The Netherlands.
    /// 
    /// Written by Marcel de Wijs with help from a lot of others, 
    /// especially Stefan Nelwan
    /// 
    /// This code is for free. I ported it from several different sources to C#.
    /// 
    /// For comments: Marcel_de_Wijs@hotmail.com
    /// ***************************************************************************
    /// </summary>
	public class CRCTool
	{
        // 'order' [1..32] is the CRC polynom order, counted without the leading '1' bit
        // 'polynom' is the CRC polynom without leading '1' bit
        // 'direct' [0,1] specifies the kind of algorithm: 1=direct, no augmented zero bits
        // 'crcinit' is the initial CRC value belonging to that algorithm
        // 'crcxor' is the final XOR value
        // 'refin' [0,1] specifies if a data byte is reflected before processing (UART) or not
        // 'refout' [0,1] specifies if the CRC will be reflected before XOR
        // Data character string
        // For CRC-CCITT : order = 16, direct=1, poly=0x1021, CRCinit = 0xFFFF, crcxor=0; refin =0, refout=0  
        // For CRC16:      order = 16, direct=1, poly=0x8005, CRCinit = 0x0, crcxor=0x0; refin =1, refout=1  
        // For CRC32:      order = 32, direct=1, poly=0x4c11db7, CRCinit = 0xFFFFFFFF, crcxor=0xFFFFFFFF; refin =1, refout=1  
        // Default : CRC-CCITT

        private int   order      = 16;
        private ulong polynom    = 0x1021;
        private int   direct     = 1;
        private ulong crcinit    = 0xFFFF;
        private ulong crcxor     = 0x0;
        private int   refin      = 0;
        private int   refout     = 0;
        
        private ulong crcmask;
        private ulong crchighbit;
        private ulong crcinit_direct;
        private ulong crcinit_nondirect;
        private ulong [] crctab = new ulong[256];

        // Enumeration used in the init function to specify which CRC algorithm to use
        public enum CRCCode{CRC_CCITT, CRC16, CRC32};

		public CRCTool()
		{
			// 
			// TODO: Add constructor logic here
			//
		}

        public void Init(CRCCode CodingType)
        {
            switch(CodingType)
            {
                case CRCCode.CRC_CCITT:
                    order = 16; direct=1; polynom=0x1021; crcinit = 0xFFFF; crcxor=0; refin =0; refout=0;
                    break;
                case CRCCode.CRC16:
                    order = 16; direct=1; polynom=0x8005; crcinit = 0x0; crcxor=0x0; refin =1; refout=1;  
                    break;
                case CRCCode.CRC32:
                    order = 32; direct=1; polynom=0x4c11db7; crcinit = 0xFFFFFFFF; crcxor=0xFFFFFFFF; refin =1; refout=1;  
                    break;
            }
            
            // Initialize all variables for seeding and builing based upon the given coding type
            // at first, compute constant bit masks for whole CRC and CRC high bit
            
            crcmask = ((((ulong)1<<(order-1))-1)<<1)|1;
            crchighbit = (ulong)1<<(order-1);

            // generate lookup table
            generate_crc_table();

            ulong bit, crc;
            int i;
            if (direct == 0) 
            {
                crcinit_nondirect = crcinit;
                crc = crcinit;
                for (i=0; i<order; i++) 
                {
                    bit = crc & crchighbit;
                    crc<<= 1;
                    if (bit != 0) 
                    {
                        crc^= polynom;
                    }
                }
                crc&= crcmask;
                crcinit_direct = crc;
            }
            else 
            {
                crcinit_direct = crcinit;
                crc = crcinit;
                for (i=0; i<order; i++) 
                {
                    bit = crc & 1;
                    if (bit != 0) 
                    {
                        crc^= polynom;
                    }
                    crc >>= 1;
                    if (bit != 0) 
                    {
                        crc|= crchighbit;
                    }
                }	
                crcinit_nondirect = crc;
            }
        }


        /// <summary>
        /// 4 ways to calculate the crc checksum. If you have to do a lot of encoding
        /// you should use the table functions. Since they use precalculated values, which 
        /// saves some calculating.
        /// </summary>.
        [CLSCompliant(false)]
        public ulong crctablefast (byte[] p) 
        {
            // fast lookup table algorithm without augmented zero bytes, e.g. used in pkzip.
            // only usable with polynom orders of 8, 16, 24 or 32.
            ulong crc = crcinit_direct;
            if (refin != 0)
            {
                crc = reflect(crc, order);
            }
            if (refin == 0) 
            {
                for (int i = 0; i < p.Length; i++)
                {
                    crc = (crc << 8) ^ crctab[ ((crc >> (order-8)) & 0xff) ^ p[i]];
                }
            }
            else 
            {
                for (int i = 0; i < p.Length; i++)
                {
                    crc = (crc >> 8) ^ crctab[ (crc & 0xff) ^ p[i]];
                }
            }
            if ((refout^refin) != 0) 
            {
                crc = reflect(crc, order);
            }
            crc^= crcxor;
            crc&= crcmask;
            return(crc);
        }

		[CLSCompliant(false)]
        public ulong crctable (byte[] p) 
        {
            // normal lookup table algorithm with augmented zero bytes.
            // only usable with polynom orders of 8, 16, 24 or 32.
            ulong crc = crcinit_nondirect;
            if (refin != 0) 
            {
                crc = reflect(crc, order);
            }
            if (refin == 0) 
            {
                for (int i = 0; i < p.Length; i++)
                {
                    crc = ((crc << 8) | p[i]) ^ crctab[ (crc >> (order-8)) & 0xff ];
                }
            }
            else 
            {
                for (int i = 0; i < p.Length; i++)
                {
                    crc = (ulong)(((int)(crc >> 8) | (p[i] << (order-8))) ^ (int)crctab[ crc & 0xff ]);
                }
            }
            if (refin == 0) 
            {
                for (int i = 0; i < order/8; i++)
                {
                    crc = (crc << 8) ^ crctab[ (crc >> (order-8))  & 0xff];
                } 
            }
            else 
            {
                for (int i = 0; i < order/8; i++)
                {
                    crc = (crc >> 8) ^ crctab[crc & 0xff];
                } 
            }

            if ((refout^refin) != 0) 
            {
                crc = reflect(crc, order);
            }
            crc^= crcxor;
            crc&= crcmask;

            return(crc);
        }

		[CLSCompliant(false)]
        public ulong crcbitbybit(byte[] p) 
        {
            // bit by bit algorithm with augmented zero bytes.
            // does not use lookup table, suited for polynom orders between 1...32.
            int i;
            ulong  j, c, bit;
            ulong crc = crcinit_nondirect;

            for (i=0; i<p.Length; i++) 
            {
                c = (ulong)p[i];
                if (refin != 0) 
                {
                    c = reflect(c, 8);
                }

                for (j=0x80; j != 0; j>>=1) 
                {
                    bit = crc & crchighbit;
                    crc<<= 1;
                    if ((c & j) != 0) 
                    {
                        crc|= 1;
                    }
                    if (bit  != 0) 
                    {
                        crc^= polynom;
                    }
                }
            }	

            for (i=0; (int)i < order; i++) 
            {

                bit = crc & crchighbit;
                crc<<= 1;
                if (bit != 0) crc^= polynom;
            }

            if (refout != 0) 
            {
                crc=reflect(crc, order);
            }
            crc^= crcxor;
            crc&= crcmask;

            return(crc);
        }

		[CLSCompliant(false)]
        public ulong crcbitbybitfast(byte[] p) 
        {
            // fast bit by bit algorithm without augmented zero bytes.
            // does not use lookup table, suited for polynom orders between 1...32.
            int i;
            ulong j, c, bit;
            ulong crc = crcinit_direct;

            for (i = 0; i < p.Length; i++) 
            {
                c = (ulong)p[i];
                if (refin != 0) 
                {
                    c = reflect(c, 8);
                }

                for (j = 0x80; j > 0; j >>= 1) 
                {
                    bit = crc & crchighbit;
                    crc <<= 1;
                    if ((c & j) > 0) bit^= crchighbit;
                    if (bit > 0) crc^= polynom;
                }
            }	

            if (refout > 0) 
            {
                crc=reflect(crc, order);
            }
            crc^= crcxor;
            crc&= crcmask;

            return(crc);
        }

    
        /// <summary>
        /// CalcCRCITT is an algorithm found on the web for calculating the CRCITT checksum
        /// It is included to demonstrate that although it looks different it is the same 
        /// routine as the crcbitbybit* functions. But it is optimized and preconfigured for CRCITT.
        /// </summary>
        [CLSCompliant(false)]
        public ushort CalcCRCITT(byte[] p)
        {
            uint uiCRCITTSum = 0xFFFF;
            uint uiByteValue;

            for (int iBufferIndex = 0; iBufferIndex < p.Length; iBufferIndex++)
            {
                uiByteValue = ((uint) p[iBufferIndex] << 8);
                for (int iBitIndex = 0; iBitIndex < 8; iBitIndex++)
                {
                    if (((uiCRCITTSum^uiByteValue) & 0x8000) != 0)
                    {
                        uiCRCITTSum = (uiCRCITTSum <<1) ^ 0x1021;
                    }
                    else
                    {
                        uiCRCITTSum <<= 1;
                    }
                    uiByteValue <<=1;
                }
            }
            return (ushort)uiCRCITTSum;
        }
  

        #region subroutines
        private ulong reflect (ulong crc, int bitnum) 
        {

            // reflects the lower 'bitnum' bits of 'crc'

            ulong i, j=1, crcout = 0;

            for (i = (ulong)1 <<(bitnum-1); i != 0; i>>=1) 
            {
                if ((crc & i) != 0) 
                {
                    crcout |= j;
                }
                j<<= 1;
            }
            return (crcout);
        }

        private void generate_crc_table() 
        {

            // make CRC lookup table used by table algorithms

            int i, j;
            ulong bit, crc;

            for (i=0; i<256; i++) 
            {
                crc=(ulong)i;
                if (refin !=0) 
                {
                    crc=reflect(crc, 8);
                }
                crc<<= order-8;

                for (j=0; j<8; j++) 
                {
                    bit = crc & crchighbit;
                    crc<<= 1;
                    if (bit !=0) crc^= polynom;
                }			

                if (refin != 0) 
                {
                    crc = reflect(crc, order);
                }
                crc&= crcmask;
                crctab[i]= crc;
            }
        }
        #endregion 
    }
}
