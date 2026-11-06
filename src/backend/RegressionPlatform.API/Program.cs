using RegressionPlatform.API.Middleware;
using RegressionPlatform.Application;
using RegressionPlatform.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging via Serilog, configured from appsettings.
builder.Host.UseSerilog(
    (context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services)
);

// Presentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddHealthChecks();

// RFC 7807 problem details + global exception handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// CORS — allow the Angular app origins listed in config (Cors:AllowedOrigins). Covers the local
// dev app (http://localhost:4200) and the deployed dev frontend (https://dev-core-service.innova.com).
// Note: an origin is scheme+host+port only — the frontend's "/regression" base path is not part of it.
const string DevCorsPolicy = "DevCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
    options.AddPolicy(
        DevCorsPolicy,
        policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()
    )
);

// Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(DevCorsPolicy);
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
