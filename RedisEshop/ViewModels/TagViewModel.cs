using RedisEshop.Entities;

namespace RedisEshop.ViewModels
{
    public class TagViewModel
    {
		public int TagId { get; set; }
		public string Identifier { get; set; }
		public string Title { get; set; }
		public TagType Type {get; set;}
    }
}
