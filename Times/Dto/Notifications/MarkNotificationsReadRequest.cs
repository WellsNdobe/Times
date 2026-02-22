using System;
using System.Collections.Generic;

namespace Times.Dto.Notifications
{
	public class MarkNotificationsReadRequest
	{
		public List<Guid> Ids { get; set; } = new();
	}
}
