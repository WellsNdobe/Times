using System;

namespace Times.Services.Errors
{
	public abstract class AppException : Exception
	{
		public string Code { get; }

		protected AppException(string code, string message, Exception? inner = null)
			: base(message, inner)
		{
			Code = code;
		}
	}
}
