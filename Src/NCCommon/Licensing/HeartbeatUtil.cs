using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Licensing.Crypto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Licensing
{
    public class HeartbeatUtil
    {
        public static readonly string SkippedHeartbeatFilePath = Path.Combine(AppUtil.InstallDir, "bin", "service", "hb.bin");
        public static readonly string SkippedHeartbeatTempFilePath = Path.Combine(AppUtil.InstallDir, "bin", "service", ".temp.bin");

        public static void CleanSkippedHeartbeatFile()
        {
            SkippedHeartbeats skippedHeartbeats = new SkippedHeartbeats();
            string serializedHeartbeats = Newtonsoft.Json.JsonConvert.SerializeObject(skippedHeartbeats);

            // Clear file before writing
            File.WriteAllText(SkippedHeartbeatFilePath, string.Empty);

            // create file with empty skipped heartbeat, this code block is in using to make sure that the lock on file is release after creation.
            using (BinaryWriter binWriter = new BinaryWriter(File.OpenWrite(SkippedHeartbeatFilePath)))
            {
                string encodedHeartbeats = NCCryptoCode.Encode(serializedHeartbeats);
                binWriter.Write(encodedHeartbeats);

                binWriter.Flush();
            }
        }

        // Returns the count of skipped heartbeat in hb.bin / temp.bin
        public static int GetSkippedHeartbeatsCount()
        {
            int hbCount = 0;

            SkippedHeartbeats skippedHeartbeats = ReadSkippedHeartbeatsFromFile();

            if (skippedHeartbeats != null && skippedHeartbeats.Heartbeats != null)
                hbCount = skippedHeartbeats.Heartbeats.Count;


            return hbCount;
        }

        public static SkippedHeartbeats ReadSkippedHeartbeatsFromFile()
        {
            SkippedHeartbeats skippedHeartbeats = new SkippedHeartbeats();

            // Try reading from hb.bin
            try
            {
                if (File.Exists(SkippedHeartbeatFilePath))
                {
                    using (BinaryReader binReader = new BinaryReader(File.OpenRead(SkippedHeartbeatFilePath)))
                    {
                        if (binReader.BaseStream.Length > 0)
                        {
                            string encodedString = binReader.ReadString();
                            skippedHeartbeats = JsonConvert.DeserializeObject<SkippedHeartbeats>(NCCryptoCode.Decode(encodedString).ToString());
                            return skippedHeartbeats;
                        }
                    }
                }
            }
            catch (Exception) { }

            // If unable to parse from hb.bin, try reading from temp file
            try
            {
                if (File.Exists(SkippedHeartbeatTempFilePath))
                {
                    using (BinaryReader binReader = new BinaryReader(File.OpenRead(SkippedHeartbeatTempFilePath)))
                    {
                        if (binReader.BaseStream.Length > 0)
                        {
                            string encodedString = binReader.ReadString();
                            skippedHeartbeats = JsonConvert.DeserializeObject<SkippedHeartbeats>(NCCryptoCode.Decode(encodedString).ToString());
                            return skippedHeartbeats;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                NCacheServiceLogger.LogError($"Error occured while reading from hb.bin. {e}");
            }

            return skippedHeartbeats;
        }
    }
}
