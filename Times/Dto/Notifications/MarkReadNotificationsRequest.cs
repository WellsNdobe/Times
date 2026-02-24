using System;
using System.Collections.Generic;

namespace Times.Dto.Notifications
{
	public class MarkReadNotificationsRequest
	{
		public List<Guid> Ids { get; set; } = new();
	}
}

