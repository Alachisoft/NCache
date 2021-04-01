using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.FeatureUsageData
{
    public sealed class FeatureUsageCollector
    {
        private Feature[] _featuresInUse;
        private static object _creationLock = new object();
        static FeatureUsageCollector _featureUsageCollector = null;
        public const string FeatureTag = "$FeatureTag#";


        public static FeatureUsageCollector Instance
        {
            get
            {
                if (_featureUsageCollector == null)
                {
                    lock (_creationLock)
                    {
                        if (_featureUsageCollector == null)
                            _featureUsageCollector = new FeatureUsageCollector();
                    }
                }
                return _featureUsageCollector;
            }
        }

        private FeatureUsageCollector()
        {
            _featuresInUse = new Feature[System.Enum.GetValues(typeof(FeatureEnum)).Length];
        }


        public Feature GetFeature(FeatureEnum featureEnum, FeatureEnum parentFeatureEnum = FeatureEnum.none)
        {
            var featureRequired = _featuresInUse[(int)featureEnum];
            if (featureRequired == null)
            {
                featureRequired = new Feature() { Name = featureEnum.ToString(), ID = (short)featureEnum, CreationTime = DateTime.Now, LastUsageTime = DateTime.Now };
                _featuresInUse[(int)featureEnum] = featureRequired;
            }

            if (parentFeatureEnum != FeatureEnum.none)
            {
                Feature parent = _featuresInUse[(int)parentFeatureEnum];

                if (parent == null)
                {
                    parent = new Feature() { Name = parentFeatureEnum.ToString(), ID = (short)parentFeatureEnum, CreationTime = DateTime.Now, LastUsageTime = DateTime.Now };
                    _featuresInUse[(int)parentFeatureEnum] = parent;
                }
                featureRequired.ParentFeature = parent;
            }
            return featureRequired;
        }

        public Dictionary<string, Feature> GetFeatureReport()
        {
            Dictionary<string, Feature> featureDictionary = new Dictionary<string, Feature>();

            for (int i = 0; i < _featuresInUse.Length; i++)
            {
                Feature feature = _featuresInUse[i];

                if (feature == null) continue;

                feature = AddChildInHierarchy(feature);

                if (!featureDictionary.ContainsKey(feature.Name))
                    featureDictionary.Add(feature.Name, feature);
                else
                    featureDictionary[feature.Name] = feature;
            }

            return featureDictionary;
        }

        private Feature AddChildInHierarchy(Feature feature)
        {
            Feature parentFeature = feature.ParentFeature;

            if (parentFeature == null)
                return feature;

            parentFeature.AddChildFeature(feature);
            parentFeature = AddChildInHierarchy(parentFeature);

            return parentFeature;
        }

        public Feature GetClientFeature(string applicationName)
        {
            string feature = String.Empty;
            if (applicationName.Contains(FeatureUsageCollector.FeatureTag))
            {
                feature = applicationName.Split('#')[1];
            }

            FeatureEnum featureEnum = FeatureEnum.none;

            if (feature.Equals(FeatureEnum.aspnet_session.ToString()))
                featureEnum = FeatureEnum.aspnet_session;
            else if (feature.Equals(FeatureEnum.aspnetcore_session.ToString()))
                featureEnum = FeatureEnum.aspnetcore_session;
            else if (feature.Equals(FeatureEnum.efcore.ToString()))
                featureEnum = FeatureEnum.efcore;
            else if (feature.Equals(FeatureEnum.efcore61.ToString()))
                featureEnum = FeatureEnum.efcore61;
            else if (feature.Equals(FeatureEnum.hibernate.ToString()))
                featureEnum = FeatureEnum.hibernate;
            else if (feature.Equals(FeatureEnum.outputcache_provider.ToString()))
                featureEnum = FeatureEnum.outputcache_provider;
            else if (feature.Equals(FeatureEnum.view_state.ToString()))
                featureEnum = FeatureEnum.view_state;

            return GetFeature(featureEnum);
        }
    }
}
