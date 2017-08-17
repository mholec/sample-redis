using System.Collections.Generic;

namespace RedisEshop.Entities
{
	public class Tag
	{
		public int TagId { get; set; }
		public string Identifier { get; set; }
		public string Title { get; set; }
		public TagType Type {get; set;}

		public ICollection<ProductTag> ProductTags { get; set; }
	}
}