using FarmTrackBE.Services;
using FarmTrackBE.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using FirebaseAdmin;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using FirebaseAdmin.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

try
{
    // Create a logger for initialization
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.AddDebug();
    });
    var logger = loggerFactory.CreateLogger<Program>();

    // Log configuration values to help diagnose issues
    LogConfigurationValues(builder.Configuration, logger);

    // Check if we have API key in user secrets
    var apiKey = builder.Configuration["Firebase:ApiKey"];
    if (string.IsNullOrEmpty(apiKey))
    {
        logger.LogWarning("Firebase:ApiKey not found in configuration. Please set it using:");
        logger.LogWarning("dotnet user-secrets set \"Firebase:ApiKey\" \"your-firebase-api-key\"");
    }
    else
    {
        logger.LogInformation("Firebase:ApiKey found in configuration");
    }

    // Check if we have ProjectId in user secrets
    var projectId = builder.Configuration["Firebase:ProjectId"];
    if (string.IsNullOrEmpty(projectId))
    {
        logger.LogWarning("Firebase:ProjectId not found in configuration. Please set it using:");
        logger.LogWarning("dotnet user-secrets set \"Firebase:ProjectId\" \"farmtrackbe\"");
    }
    else
    {
        logger.LogInformation($"Firebase:ProjectId found in configuration: {projectId}");
    }

    // Try to directly initialize Firebase with the service account file
    var secretsDir = Path.Combine(Directory.GetCurrentDirectory(), "secrets");
    var serviceAccountPath = Path.Combine(secretsDir, "firebase-service-account.json");

    // Create secrets directory if it doesn't exist
    if (!Directory.Exists(secretsDir))
    {
        logger.LogInformation($"Creating secrets directory: {secretsDir}");
        Directory.CreateDirectory(secretsDir);
    }

    if (File.Exists(serviceAccountPath))
    {
        logger.LogInformation($"Found firebase-service-account.json at: {serviceAccountPath}");

        try
        {
            // Set the path in configuration for FirebaseInitializer to use
            builder.Configuration["Firebase:ServiceAccountPath"] = serviceAccountPath;

            // Get project ID from the file if not already set
            if (string.IsNullOrEmpty(projectId))
            {
                var json = File.ReadAllText(serviceAccountPath);
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("project_id", out var projectIdElement))
                {
                    projectId = projectIdElement.GetString();
                    builder.Configuration["Firebase:ProjectId"] = projectId;
                    logger.LogInformation($"Set Firebase:ProjectId to {projectId} from service account file");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading firebase-service-account.json");
        }
    }
    else
    {
        logger.LogWarning($"Service account file not found at: {serviceAccountPath}");
        logger.LogInformation("Please create this file with your Firebase service account credentials");
        logger.LogInformation("You can download it from the Firebase Console > Project Settings > Service accounts");
    }

    // Initialize Firebase
    FirebaseInitializer.Initialize(builder.Configuration, logger);

    // Now register Firestore as a singleton
    builder.Services.AddSingleton(FirebaseInitializer.FirestoreDb);

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();
    ConfigureSwagger(builder.Services);
    ConfigureCors(builder.Services);
    RegisterApplicationServices(builder.Services);

    var app = builder.Build();
    ConfigureMiddleware(app);
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Avvio applicazione fallito: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}

void LogConfigurationValues(IConfiguration config, ILogger logger)
{
    logger.LogInformation("Checking Firebase configuration values:");
    logger.LogInformation($"FIREBASE_CONFIG: {(string.IsNullOrEmpty(config["FIREBASE_CONFIG"]) ? "Not found" : "Found")}");
    logger.LogInformation($"FIREBASE_PROJECT_ID: {(string.IsNullOrEmpty(config["FIREBASE_PROJECT_ID"]) ? "Not found" : config["FIREBASE_PROJECT_ID"])}");
    logger.LogInformation($"Firebase:ProjectId: {(string.IsNullOrEmpty(config["Firebase:ProjectId"]) ? "Not found" : config["Firebase:ProjectId"])}");
    logger.LogInformation($"Firebase:ApiKey: {(string.IsNullOrEmpty(config["Firebase:ApiKey"]) ? "Not found" : "Found")}");
    logger.LogInformation($"Firebase:ServiceAccountPath: {(string.IsNullOrEmpty(config["Firebase:ServiceAccountPath"]) ? "Not found" : config["Firebase:ServiceAccountPath"])}");

    // Check for secrets directory and firebase-service-account.json file
    var secretsDir = Path.Combine(Directory.GetCurrentDirectory(), "secrets");
    var serviceAccountPath = Path.Combine(secretsDir, "firebase-service-account.json");

    logger.LogInformation($"Current directory: {Directory.GetCurrentDirectory()}");
    logger.LogInformation($"Secrets directory exists: {Directory.Exists(secretsDir)}");
    logger.LogInformation($"firebase-service-account.json exists: {File.Exists(serviceAccountPath)}");

    if (File.Exists(serviceAccountPath))
    {
        try
        {
            var json = File.ReadAllText(serviceAccountPath);
            logger.LogInformation($"firebase-service-account.json is valid JSON: {IsValidJson(json)}");

            if (IsValidJson(json))
            {
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.TryGetProperty("project_id", out var projectIdElement))
                {
                    logger.LogInformation($"Project ID in firebase-service-account.json: {projectIdElement.GetString()}");
                }
                else
                {
                    logger.LogWarning("No project_id found in firebase-service-account.json");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading firebase-service-account.json");
        }
    }
}

bool IsValidJson(string json)
{
    try
    {
        JsonDocument.Parse(json);
        return true;
    }
    catch
    {
        return false;
    }
}

string GetFirebaseConfiguration(IConfiguration config, IHostEnvironment env)
{
    var firebaseConfig = config["FIREBASE_CONFIG"];

    if (string.IsNullOrEmpty(firebaseConfig) && env.IsDevelopment())
    {
        firebaseConfig = config["Firebase:ServiceAccountJson"];

        if (string.IsNullOrEmpty(firebaseConfig))
        {
            var path = config["Firebase:ServiceAccountPath"];
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                firebaseConfig = File.ReadAllText(path);
            }
        }
    }

    if (string.IsNullOrEmpty(firebaseConfig))
    {
        throw new ApplicationException(
            "Configurazione Firebase mancante. Verifica:\n" +
            "1. Variabile d'ambiente FIREBASE_CONFIG\n" +
            "2. User secrets (Development):\n" +
            "   - dotnet user-secrets set \"Firebase:ApiKey\" \"your-firebase-api-key\"\n" +
            "   - dotnet user-secrets set \"Firebase:ProjectId\" \"farmtrackbe\"\n" +
            "3. File in Firebase:ServiceAccountPath (Development)");
    }

    try
    {
        JsonDocument.Parse(firebaseConfig);
        return firebaseConfig;
    }
    catch (JsonException ex)
    {
        throw new ApplicationException("Configurazione Firebase non valida. Verifica il formato JSON.", ex);
    }
}

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

    app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        firebase = FirebaseApp.DefaultInstance != null ? "Connected" : "Disconnected",
        firebaseAuth = FirebaseAuth.DefaultInstance != null ? "Available" : "Unavailable",
        firestore = FirebaseInitializer.FirestoreDb != null ? "Connected" : "Disconnected",
        timestamp = DateTime.UtcNow
    }));
}
