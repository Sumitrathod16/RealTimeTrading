using Microsoft.EntityFrameworkCore;
using Trading.Api.Configuration;
using Trading.Api.Data;
using Trading.Api.Hubs;
using Trading.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Map .env-style variables: AUTH_USER_ID -> Auth:UserId
MapEnv(builder.Configuration, "AUTH_URL", "ExternalApi:AuthUrl");
MapEnv(builder.Configuration, "WS_URL", "ExternalApi:WebSocketUrl");
MapEnv(builder.Configuration, "USER_ID", "Auth:UserId");
MapEnv(builder.Configuration, "ACCOUNT_ID", "Auth:AccountId");
MapEnv(builder.Configuration, "PASSWORD", "Auth:Password");

builder.Services.Configure<ExternalApiOptions>(builder.Configuration.GetSection(ExternalApiOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TradingDb")));

builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddSingleton<IPriceCache, PriceCache>();
builder.Services.AddSingleton<ConnectionStateTracker>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddSingleton<IFeedNotifier, SignalRFeedNotifier>();
builder.Services.AddHostedService<PriceFeedWorker>();

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.MapHub<TradingHub>("/hubs/trading");

app.Logger.LogInformation("Trading API started on {Urls}", string.Join(", ", app.Urls));
app.Run();

static void MapEnv(IConfiguration config, string envKey, string configKey)
{
    var value = Environment.GetEnvironmentVariable(envKey);
    if (!string.IsNullOrEmpty(value))
        config[configKey] = value;
}
