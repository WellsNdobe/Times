using System;

namespace Times.Services.Errors
{
	public sealed class ForbiddenException : AppException
	{
		public ForbiddenException(string message = "Forbidden.", Exception? inner = null)
			: base("forbidden", message, inner) { }
	}
}
