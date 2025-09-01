using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using FamilyManagement.API.Data;
using FamilyManagement.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog minimal setup
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Configuration
var configuration = builder.Configuration;

// Add DbContext (placeholder: use PostgreSQL via Supabase connection string)
var connectionString = configuration.GetConnectionString("Default")
    ?? Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION")
    ?? "Host=localhost;Database=famlio;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// CORS for local dev and deployed frontends
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:5173", "http://localhost:3000", "https://localhost:5173" };
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("default", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Authentication - Support backend-signed JWT and Supabase JWT validation
var jwtSecret = configuration["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("BACKEND_JWT_SECRET");
var supabaseIssuer = configuration["Supabase:JwtIssuer"] ?? Environment.GetEnvironmentVariable("SUPABASE_JWT_ISSUER");
var supabaseAudience = configuration["Supabase:JwtAudience"] ?? Environment.GetEnvironmentVariable("SUPABASE_JWT_AUDIENCE");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("BackendJwt", options =>
{
    if (string.IsNullOrWhiteSpace(jwtSecret))
    {
        // Provide a non-fatal placeholder to ease local scaffolding
        jwtSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes("dev-secret-change-me"));
    }
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
})
.AddJwtBearer("Supabase", options =>
{
    // When configured, uses Supabase JWTs (GoTrue). Provide Issuer + Audience
    if (!string.IsNullOrWhiteSpace(supabaseIssuer))
    {
        options.Authority = supabaseIssuer;
    }
    if (!string.IsNullOrWhiteSpace(supabaseAudience))
    {
        options.Audience = supabaseAudience;
    }
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrWhiteSpace(supabaseIssuer),
        ValidateAudience = !string.IsNullOrWhiteSpace(supabaseAudience),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization();

// Add services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISupabaseService, SupabaseService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();

builder.Services.AddSignalR();
builder.Services.AddControllers();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseCors("default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

