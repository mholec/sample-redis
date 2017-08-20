using System.IO;
using ProtoBuf;

namespace RedisEshop.Serialization
{
	public class ProtobufDistributedCacheSerializer<T> : IDistributedCacheSerializer<T>
	{
		public T Deserialize(byte[] data)
		{
			T obj = Serializer.Deserialize<T>(new MemoryStream(data));

			return obj;
		}

		public byte[] Serialize(T originalObject)
		{
			byte[] raw;
			using (MemoryStream ms = new MemoryStream())
			{
				Serializer.Serialize(ms, originalObject);
				raw = ms.ToArray();
			}

			return raw;
		}
	}
}