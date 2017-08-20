namespace RedisEshop.ViewModels
{
    public class EmailMessageViewModel
    {
	    public string To { get; set; }
	    public string Subject { get; set; }
	    public string Message { get; set; }
	    public bool IsHtml { get; set; } = true;
    }
}
