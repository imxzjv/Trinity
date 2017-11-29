using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Trinity.Components.Swarm.Internals
{
    public interface IDataSerializer
    {
        string Serialize<T>(T value) where T : class;
        T Deserialize<T>(string json) where T : class;
        T Deserialize<T>(string json, T toInstance) where T : class;    
    }

    public static class DataSerializer
    {
        public static IDataSerializer Default { get; } = new JsonDataSerializer();
        public static IDataSerializer Current { get; set; } = Default;
    }

    public class JsonDataSerializer : IDataSerializer
    {
        public string Serialize<T>(T value) where T : class
        {
            var settings = new DataContractJsonSerializerSettings
            {
                EmitTypeInformation = EmitTypeInformation.AsNeeded,
                UseSimpleDictionaryFormat = true
            };

            var serializer = new DataContractJsonSerializer(typeof(T), settings);

            string output;
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, value);
                output = Encoding.UTF8.GetString(stream.ToArray());
            }
            return output;
        }

        public T Deserialize<T>(string json) where T : class
        {
            using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return serializer.ReadObject(stream) as T;
            }
        }

        public T Deserialize<T>(string json, T instance) where T : class
        {
            using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                var newObj = serializer.ReadObject(stream) as T;
                PropertyCopy.Copy(newObj, instance);
                return instance;
            }
        }
    }
}

