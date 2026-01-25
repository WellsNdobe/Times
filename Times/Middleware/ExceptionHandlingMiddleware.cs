using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Times.Services.Errors;

namespace Times.Middleware
{
	public sealed class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;

		public ExceptionHandlingMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (AppException ex)
			{
				await WriteProblemDetails(context, ex);
			}
		}

		private static async Task WriteProblemDetails(HttpContext context, AppException ex)
		{
			context.Response.ContentType = "application/problem+json";

			var (status, title) = ex switch
			{
				UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
				ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
				NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
				ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
				ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
				_ => (StatusCodes.Status400BadRequest, "Bad Request")
			};

			context.Response.StatusCode = status;

			var problem = new ProblemDetails
			{
				Status = status,
				Title = title,
				Detail = ex.Message,
				Type = $"https://httpstatuses.com/{status}",
				Instance = context.Request.Path
			};

			problem.Extensions["code"] = ex.Code;

			if (ex is ValidationException vex && vex.Errors.Count > 0)
				problem.Extensions["errors"] = vex.Errors;

			await context.Response.WriteAsJsonAsync(problem);
		}
	}
}
