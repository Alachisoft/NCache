using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.FeatureUsageData
{
    [Serializable]
    public class Feature : ICompactSerializable, ICloneable
    {
        public Feature()
        {

        }


        public Feature ParentFeature { get; set; }

        public short ID { get; set; }


        public string Name { get; set; }

        /// <summary>
        /// Gets/Sets last usage time in UTC
        /// </summary>
        public DateTime LastUsageTime { get; set; }

        /// <summary>
        /// Gets/Sets creation time in UTC
        /// </summary>
        public DateTime CreationTime { get; set; }

        public IDictionary<string, Feature> SubFeatures { get; set; } = new Dictionary<string, Feature>();

        /// <summary>
        /// If a sub feature exists 
        /// and is to be added it will be added 
        /// in the parent feature , to mantain herarchy.
        /// </summary>
        /// <param name="featureName"></param>
        public void AddChildFeature(Feature feature)
        {
            if (!SubFeatures.ContainsKey(feature.Name))
            {
                SubFeatures.Add(feature.Name, feature);
            }
        }

        public Feature GetFeature(string featureName)
        {
            if (featureName == null)
                throw new ArgumentNullException(nameof(featureName));

            return SubFeatures[featureName] as Feature;
        }


        /// <summary>
        /// returns the list of 
        /// features added.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, Feature> Features()
        {
            if (SubFeatures.Count > 0)
            {
                return SubFeatures;
            }
            return null;
        }


        public static Common.FeatureUsageData.Dom.Feature[] ConvertFeatures(IDictionary<string, Feature> subFeatures)
        {
            if (subFeatures != null && subFeatures.Count > 0)
            {
                Common.FeatureUsageData.Dom.Feature[] features = new Common.FeatureUsageData.Dom.Feature[subFeatures.Count];
                Feature[] reportedFeatures = subFeatures.Values.ToArray();
                for (int i = 0; i < reportedFeatures.Length; i++)
                {
                    features[i] = new Dom.Feature
                    {
                        Name = reportedFeatures[i].Name,
                        LastUsedOn = reportedFeatures[i].LastUsageTime.ToString("d"),
                        CreationTime = reportedFeatures[i].CreationTime.ToString("d"),
                        Subfeatures = ConvertFeatures(reportedFeatures[i].SubFeatures)
                    };
                }

                return features;
            }
            return null;
        }
        public void UpdateUsageTime()
        {
            LastUsageTime = DateTime.Now;
        }

        public void UpdateUsageTime(DateTime dateTime)
        {
            LastUsageTime = dateTime;
        }

        #region Compact Serialization
        public void Deserialize(CompactReader reader)
        {
            Name = reader.ReadObject() as string;
            LastUsageTime = (DateTime)reader.ReadObject();
            CreationTime = (DateTime)reader.ReadObject();

        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Name);
            writer.WriteObject(LastUsageTime);
            writer.WriteObject(CreationTime);
        }

        public object Clone()
        {
            Feature feature = new Feature
            {
                Name = Name,
                LastUsageTime = LastUsageTime,
                CreationTime = CreationTime,
                SubFeatures = new Dictionary<string, Feature>(SubFeatures)
            };
            return feature;
        }
        #endregion

        public override bool Equals(Object obj)
        {
            if (!(obj is Feature feature)) return false;

            return feature.Name.Equals(Name);

        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
