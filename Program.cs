using FarmTrackBE.Services;
using FarmTrackBE.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using FirebaseAdmin;

var builder = WebApplication.CreateBuilder(args);

// Configurazione gerarchica
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

// Validazione configurazione Firebase
var firebaseConfig = builder.Configuration["FIREBASE_CONFIG"];
if (string.IsNullOrEmpty(firebaseConfig))
{
    throw new ApplicationException("Configurazione Firebase mancante. Verifica le variabili d'ambiente.");
}

try
{
    JsonDocument.Parse(firebaseConfig); // Verifica che il JSON sia valido
}
catch (JsonException ex)
{
    throw new ApplicationException("Configurazione Firebase non valida.", ex);
}

// Inizializzazione Firebase con logging
builder.Services.AddSingleton(provider =>
{
    try
    {
        FirebaseInitializer.Initialize(builder.Configuration);
        return FirebaseInitializer.FirestoreDb;
    }
    catch (Exception ex)
    {
        var logger = provider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Errore durante l'inizializzazione di Firebase");
        throw;
    }
});

// Servizi API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Configurazione Swagger
builder.Services.AddEndpointsApiExplorer();
ConfigureSwagger(builder.Services);

// CORS Policy
ConfigureCors(builder.Services);

// Registrazione servizi applicativi
RegisterApplicationServices(builder.Services);

var app = builder.Build();

// Pipeline middleware
ConfigureMiddleware(app);

app.Run();

// Metodi di configurazione locali
void ConfigureSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "FarmTrack API",
            Version = "v1",
            Description = "API per la gestione di animali, trattamenti e farmaci"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
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
                Array.Empty<string>()
            }
        });
    });
}

void ConfigureCors(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "https://tuo-frontend.onrender.com")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });
}

void RegisterApplicationServices(IServiceCollection services)
{
    services.AddScoped<AnimalService>();
    services.AddScoped<DrugService>();
    services.AddScoped<TreatmentService>();
    services.AddScoped<FirebaseAuthService>();
}

void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FarmTrack API v1");
            c.DisplayRequestDuration();
        });

        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseMiddleware<FirebaseAuthMiddleware>();
    app.MapControllers();

    // Endpoint di health check
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        firebase = FirebaseApp.DefaultInstance != null ? "Connected" : "Disconnected",
        timestamp = DateTime.UtcNow
    }));
}