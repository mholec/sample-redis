using StackExchange.Redis;

namespace RedisEshop.Services
{
    public class RedisQueue
    {
	    private const string RedisQueueKey = "redisqueues";
	    private readonly ConnectionMultiplexer _connectionMultiplexer;

	    public RedisQueue(ConnectionMultiplexer connectionMultiplexer)
	    {
		    _connectionMultiplexer = connectionMultiplexer;
	    }

	    public object Pop(string queueName)
	    {
		    queueName = RedisQueueKey + ":" + queueName;

		    RedisValue item = _connectionMultiplexer.GetDatabase().ListLeftPop(queueName + ":primary");
		    _connectionMultiplexer.GetDatabase().ListRightPush(queueName + ":secondary", item);

		    return item;
	    }

	    public void Commit(string queueName)
	    {
		   queueName = RedisQueueKey + ":" + queueName;

		    _connectionMultiplexer.GetDatabase().KeyDelete(queueName + ":secondary");
	    }

	    private void Move(string queueName)
	    {
		    queueName = RedisQueueKey + ":" + queueName;
	    }
    }
}
