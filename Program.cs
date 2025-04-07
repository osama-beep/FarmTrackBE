using FarmTrackBE.Services;
using FarmTrackBE.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

FirebaseInitializer.Initialize();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FarmTrack API",
        Version = "v1",
        Description = "API per la gestione di animali, trattamenti e farmaci"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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
            new string[] {}
        }
    });
});

// Configurazione CORS per permettere tutte le origini
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()  // Permette richieste da qualsiasi origine
              .AllowAnyMethod()  // Permette qualsiasi metodo HTTP (GET, POST, ecc.)
              .AllowAnyHeader(); // Permette qualsiasi header nella richiesta


    });
});

builder.Services.AddSingleton(_ => FirebaseInitializer.FirestoreDb);
builder.Services.AddScoped<AnimalService>();
builder.Services.AddScoped<DrugService>();
builder.Services.AddScoped<TreatmentService>();
builder.Services.AddScoped<FirebaseAuthService>();

var app = builder.Build();

// Configura Swagger sia in Development che in Production per Render
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FarmTrack API v1");
});

// Usa CORS con la policy configurata - IMPORTANTE: posizionarlo prima di altri middleware
app.UseCors("CorsPolicy");

// Middleware per HTTPS redirect in produzione
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<FirebaseAuthMiddleware>();

app.MapControllers();

// Per Render, è utile avere un endpoint di health check
app.MapGet("/health", () => "Healthy");

// Gestione della porta per Render
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    Console.WriteLine($"Usando la porta da variabile d'ambiente: {port}");
}

app.Run();
