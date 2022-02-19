using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using webapi;


var currentDirectory = Directory.GetCurrentDirectory();
var logDirectory = Directory.GetParent(currentDirectory.ToString()) + "/Logs/IDP_Locallog_.txt";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(logDirectory, rollingInterval: RollingInterval.Day)
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();



// Configure the HTTP request pipeline.

app.UseSwagger();
string LeftMiddleware = "app.UseSwagger();";
string RightMiddleware = "app.UseSwaggerUI();";
app.UseMiddleware<RequestResponseLoggingMiddleware>(LeftMiddleware, RightMiddleware);

app.UseSwaggerUI();
app.UseMiddleware<RequestResponseLoggingMiddleware>("app.UseSwaggerUI();", "app.UseHttpsRedirection();");
app.UseHttpsRedirection();
app.UseMiddleware<RequestResponseLoggingMiddleware>("app.UseHttpsRedirection();", "app.UseStaticFiles();");
app.UseStaticFiles();
app.UseMiddleware<RequestResponseLoggingMiddleware>("app.UseStaticFiles();", "app.UseAuthorization();");
app.UseAuthorization();
app.UseMiddleware<RequestResponseLoggingMiddleware>("app.UseAuthorization();", "app.MapControllers();");
app.MapControllers();
app.UseMiddleware<RequestResponseLoggingMiddleware>("app.MapControllers();", "app.Run();");
app.Run();
app.UseMiddleware<RequestResponseLoggingMiddleware>("YOU SHOULD NEVER REACH THIS MIDDLEWARE SINCE THE ONE BEFORE IS A TERMINATOR", "");


