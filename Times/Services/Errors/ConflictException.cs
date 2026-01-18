using System;

namespace Times.Services.Errors
{
	public sealed class ConflictException : AppException
	{
		public ConflictException(string message = "Conflict.", Exception? inner = null)
			: base("conflict", message, inner) { }
	}
}
