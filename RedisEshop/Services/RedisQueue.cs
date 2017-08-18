using StackExchange.Redis;

namespace RedisEshop.Services
{
    public class RedisQueue
    {
	    private const string RedisQueueKey = "redisqueues";
	    private readonly ConnectionMultiplexer _redis;

	    public RedisQueue(ConnectionMultiplexer redis)
	    {
		    _redis = redis;
	    }

	    public object Pop(string queueName)
	    {
		    queueName = RedisQueueKey + ":" + queueName;

		    RedisValue item = _redis.GetDatabase().ListLeftPop(queueName + ":primary");
		    _redis.GetDatabase().ListRightPush(queueName + ":secondary", item);

		    return item;
	    }

	    public void Commit(string queueName)
	    {
		   queueName = RedisQueueKey + ":" + queueName;

		    _redis.GetDatabase().KeyDelete(queueName + ":secondary");
	    }

	    private void Move(string queueName)
	    {
		    queueName = RedisQueueKey + ":" + queueName;
	    }
    }
}
