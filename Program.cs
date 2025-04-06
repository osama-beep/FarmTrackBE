using FarmTrackBE.Services;
using FarmTrackBE.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration.UserSecrets;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();


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
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddSingleton(_ => FirebaseInitializer.FirestoreDb);
builder.Services.AddScoped<AnimalService>();
builder.Services.AddScoped<DrugService>();
builder.Services.AddScoped<TreatmentService>();
builder.Services.AddScoped<FirebaseAuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FarmTrack API v1");
    });
}

app.UseCors("AllowAll");

app.UseMiddleware<FirebaseAuthMiddleware>();

app.MapControllers();
app.Run();
