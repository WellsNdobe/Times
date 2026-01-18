using System;

namespace Times.Services.Errors
{
	public sealed class NotFoundException : AppException
	{
		public NotFoundException(string message = "Not found.", Exception? inner = null)
			: base("not_found", message, inner) { }
	}
}
