using System.Collections.Generic;
using System.Linq;
using RedisEshop.Entities;
using RedisEshop.ViewModels;

namespace RedisEshop.Mapping
{
	public static class TagMappingExtensions
	{
		public static List<TagViewModel> ToViewModel(this IQueryable<Tag> tags)
		{
			return tags.Select(x => new TagViewModel()
			{
				TagId = x.TagId,
				Title = x.Title,
				Identifier = x.Identifier,
				Type = x.Type
			}).ToList();
		}
	}
}