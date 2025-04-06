using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;

namespace FarmTrackBE.Services
{
    public static class FirebaseInitializer
    {
        public static FirestoreDb FirestoreDb { get; private set; }
        public static FirebaseApp FirebaseApp { get; private set; }
        private static ILogger _logger;

        public static void Initialize(IConfiguration config, ILogger logger = null)
        {
            _logger = logger;

            try
            {
                _logger?.LogInformation("Inizializzazione Firebase in corso...");

                // Check if Firebase is already initialized
                if (FirebaseApp.DefaultInstance != null)
                {
                    _logger?.LogInformation("Firebase già inizializzato");
                    FirebaseApp = FirebaseApp.DefaultInstance;

                    // Make sure we also have Firestore
                    if (FirestoreDb == null)
                    {
                        var configProjectId = config["Firebase:ProjectId"];
                        if (!string.IsNullOrEmpty(configProjectId))
                        {
                            FirestoreDb = FirestoreDb.Create(configProjectId);
                        }
                    }

                    return;
                }

                if (TryInitializeFromEnvVars(config) || TryInitializeFromJsonFile(config))
                {
                    _logger?.LogInformation("Firebase inizializzato con successo");
                    return;
                }

                throw new InvalidOperationException("Nessuna configurazione Firebase valida trovata");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Errore critico durante l'inizializzazione di Firebase");
                throw new FirebaseInitializationException("Fallimento inizializzazione Firebase", ex);
            }
        }

        private static bool TryInitializeFromEnvVars(IConfiguration config)
        {
            var projectId = config["FIREBASE_PROJECT_ID"] ?? config["Firebase:ProjectId"] ?? config["Firebase__ProjectId"];
            var serviceAccountJson = config["FIREBASE_CONFIG"];

            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(serviceAccountJson))
            {
                _logger?.LogInformation("Configurazione Firebase da variabili d'ambiente non trovata");
                return false;
            }

            try
            {
                _logger?.LogInformation("Tentativo di inizializzazione Firebase da variabili d'ambiente");

                // Verify JSON is valid
                JsonDocument.Parse(serviceAccountJson);

                // Create Firebase app
                FirebaseApp = FirebaseAdmin.FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(serviceAccountJson),
                    ProjectId = projectId
                });

                // Create Firestore DB
                FirestoreDb = FirestoreDb.Create(projectId);

                _logger?.LogInformation("Firebase configurato da variabili d'ambiente");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Fallito tentativo di inizializzazione da variabili d'ambiente");
                return false;
            }
        }

        private static bool TryInitializeFromJsonFile(IConfiguration config)
        {
            var projectId = config["Firebase:ProjectId"] ?? config["Firebase__ProjectId"];
            var jsonPath = config["Firebase:ServiceAccountPath"] ?? config["Firebase__ServiceAccountPath"];

            if (string.IsNullOrEmpty(jsonPath))
            {
                // Try default location
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "secrets", "firebase-service-account.json");
                _logger?.LogInformation($"ServiceAccountPath not specified, trying default: {jsonPath}");
            }

            if (!Path.IsPathRooted(jsonPath))
            {
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), jsonPath);
            }

            if (string.IsNullOrEmpty(projectId) || !File.Exists(jsonPath))
            {
                _logger?.LogWarning($"File di configurazione non trovato: {jsonPath} o ProjectId mancante");
                return false;
            }

            try
            {
                _logger?.LogInformation($"Tentativo di inizializzazione Firebase da file: {jsonPath}");

                FirebaseApp = FirebaseAdmin.FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(jsonPath),
                    ProjectId = projectId
                });

                FirestoreDb = FirestoreDb.Create(projectId);
                _logger?.LogInformation("Firebase configurato da file locale");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Fallito tentativo di inizializzazione da file locale");
                return false;
            }
        }
    }

    public class FirebaseInitializationException : Exception
    {
        public FirebaseInitializationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
