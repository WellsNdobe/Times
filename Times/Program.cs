using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Times.Database;
using Times.Infrastructure.Auth;
using Times.Infrastructure.Persistence;
using Times.Middleware;
using Times.Services.Contracts;
using Times.Services.Implementation;





var builder = WebApplication.CreateBuilder(args);

//Add Services
builder.Services.AddControllers();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<IRevokedTokenStore, RevokedTokenStore>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
			)
		};
		options.Events = new JwtBearerEvents
		{
			OnTokenValidated = async ctx =>
			{
				var store = ctx.HttpContext.RequestServices.GetRequiredService<IRevokedTokenStore>();
				var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
				if (!string.IsNullOrEmpty(jti) && await store.IsRevokedAsync(jti))
					ctx.Fail("Token has been revoked (logged out).");
			}
		};
	});

builder.Services.AddAuthorization();
builder.Services.AddDbContext<DataContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("Default")
	)
);
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<ITimesheetEntryService, TimesheetEntryService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddOpenApi();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<DataContext>();
	await DbSeeder.SeedAdminAsync(db);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
