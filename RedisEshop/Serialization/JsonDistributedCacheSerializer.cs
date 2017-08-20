using System.Text;
using Newtonsoft.Json;

namespace RedisEshop.Serialization
{
	public class JsonDistributedCacheSerializer<T> : IDistributedCacheSerializer<T>
	{
		public T Deserialize(byte[] data)
		{
			string jsonData = Encoding.UTF8.GetString(data);

			return JsonConvert.DeserializeObject<T>(jsonData);
		}

		public byte[] Serialize(T originalObject)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(originalObject));

			return bytes;
		}
	}
}
