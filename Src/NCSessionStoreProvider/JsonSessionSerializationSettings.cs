using Alachisoft.NCache.Web.SessionState;
using Newtonsoft.Json;

namespace Alachisoft.NCache.Web.SessionStoreProvider
{
    public sealed class JsonSessionSerializationSettings
    {
        private JsonSerializerSettings _settings;
        private static JsonSessionSerializationSettings instance = null;

        private JsonSessionSerializationSettings()
        {
            _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented,
                SerializationBinder = new NetCoreSerializationBinder(),
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
        }

        public static JsonSerializerSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new JsonSessionSerializationSettings();
                }
                return instance._settings;
            }
        }

    }
}
