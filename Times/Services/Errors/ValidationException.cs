using System;
using System.Collections.Generic;

namespace Times.Services.Errors
{
	public sealed class ValidationException : AppException
	{
		public IDictionary<string, string[]> Errors { get; }

		public ValidationException(string message, IDictionary<string, string[]>? errors = null, Exception? inner = null)
			: base("validation_error", message, inner)
		{
			Errors = errors ?? new Dictionary<string, string[]>();
		}
	}
}
