namespace RedisEshop.Serialization
{
	public interface IDistributedCacheSerializer<T>
	{
		T Deserialize(byte[] data);
		byte[] Serialize(T originalObject);
	}
}