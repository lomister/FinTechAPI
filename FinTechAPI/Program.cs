// Program.cs
using System.Text;
using FinTechAPI.Configuration;
using FinTechAPI.Data;
using FinTechAPI.Models;
using FinTechAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext with SQL Server
builder.Services.AddDbContext<FinTechDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<FinTechDbContext>()
    .AddDefaultTokenProviders();

// Add ReportingService to the container
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IReportingService, ReportingService>(); 
builder.Services.AddScoped<ISecurityService, SecurityService>(); // Изменено
builder.Services.AddScoped<ITransactionService, TransactionService>();

// Configure Authentication
var authSettingsSection = builder.Configuration.GetSection("AuthSettings");
builder.Services.Configure<AuthSettings>(authSettingsSection);

var authSettings = authSettingsSection.Get<AuthSettings>();
if (authSettings == null || string.IsNullOrEmpty(authSettings.SecretKey))
{
    throw new InvalidOperationException("Authentication settings are not configured properly.");
}

var key = Encoding.ASCII.GetBytes(authSettings.SecretKey);


builder.Services.AddAuthentication(options =>
    { 
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authSettings.Issuer,
            ValidAudience = authSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
        };
        options.SaveToken = true;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["Authorization"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinTechAPI", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

// Add CORS service
builder.Services.AddCors(options =>
{
    options.AddPolicy("MauiPolicy", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinTechAPI v1");
        c.RoutePrefix =
            string.Empty; // Установите RoutePrefix в пустую строку, чтобы Swagger был доступен по корневому URL
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseCors("MauiPolicy");

app.Run();
