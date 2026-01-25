using Times.Services.Errors;

public sealed class UnauthorizedException : AppException
{
	public UnauthorizedException(string message = "Unauthorized.", Exception? inner = null)
		: base("unauthorized", message, inner) { }
}
