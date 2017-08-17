using Newtonsoft.Json;

namespace RedisEshop.DataServices.WithRedis
{
    public class Serializer
    {
	    public object Serialize<T>(T data)
	    {
		    JsonConvert.SerializeObject(data);

		    return null;
	    }

	    public T Deserialize<T>(object data)
	    {
		    JsonConvert.DeserializeObject<T>(data.ToString());

		    return default(T);
	    }
    }
}
