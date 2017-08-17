using System.Collections.Generic;
using RedisEshop.Entities;
using RedisEshop.Mapping;
using RedisEshop.ViewModels;

namespace RedisEshop.DataServices
{
	public class CommonListService
	{
		private readonly AppDbContext _db;

		public CommonListService(AppDbContext db)
		{
			_db = db;
		}

		public List<TagViewModel> GetAllTags()
		{
			return _db.Tags.ToViewModel();
		}

		public Dictionary<TagType, string> TagTypes()
		{
			return new Dictionary<TagType, string>()
			{
				{TagType.KindOfWeapon, "Druh zbraně"},
				{TagType.Caliber, "Ráže"},
				{TagType.Manufacturer, "Výrobce"},
			};
		}
	}
}
