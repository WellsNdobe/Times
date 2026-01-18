using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Times.Database;
using Times.Infrastructure.Auth;
using Times.Infrastructure.Persistence;
using Times.Services.Contracts;
using Times.Services.Implementation;




var builder = WebApplication.CreateBuilder(args);

//Add Services
builder.Services.AddControllers();
builder.Services.AddScoped<JwtTokenService>();
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

app.UseAuthorization();

app.MapControllers();

app.Run();
