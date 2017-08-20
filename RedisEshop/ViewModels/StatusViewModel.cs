using System;
using ProtoBuf;

namespace RedisEshop.ViewModels
{
	[ProtoContract]
    public class StatusViewModel
    {
		[ProtoMember(1)]
		public string Name { get; set; }

		[ProtoMember(2)]
		public DateTime Created { get; set; }
    }
}
