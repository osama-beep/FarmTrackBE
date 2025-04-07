using FarmTrackBE.Services;
using FarmTrackBE.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FarmTrackBE.Services.BackgroundServices;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();


    });
});

builder.Services.AddSingleton(_ => FirebaseInitializer.FirestoreDb);
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddHostedService<NotificationBackgroundService>();
builder.Services.AddScoped<AnimalService>();
builder.Services.AddScoped<DrugService>();
builder.Services.AddScoped<TreatmentService>();
builder.Services.AddScoped<FirebaseAuthService>();
builder.Services.AddScoped<ImageKitUploadService>();



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FarmTrack API v1");
});

app.UseCors("CorsPolicy");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<FirebaseAuthMiddleware>();

app.MapControllers();

app.MapGet("/health", () => "Healthy");

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    Console.WriteLine($"Usando la porta da variabile d'ambiente: {port}");
}

app.Run();
