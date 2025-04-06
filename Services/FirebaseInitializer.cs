using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace FarmTrackBE.Services
{
    public static class FirebaseInitializer
    {
        public static FirestoreDb FirestoreDb { get; private set; }

        public static void Initialize(IConfiguration config)
        {
            try
            {
                // Modalità Render (variabili d'ambiente)
                if (!string.IsNullOrEmpty(config["FIREBASE_CONFIG"]))
                {
                    InitFromEnvVars(config);
                }
                // Modalità locale (file JSON)
                else
                {
                    InitFromJsonFile(config);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRORE Firebase: {ex.Message}");
                throw;
            }
        }

        private static void InitFromEnvVars(IConfiguration config)
        {
            var projectId = config["FIREBASE_PROJECT_ID"];
            var serviceAccountJson = config["FIREBASE_CONFIG"];

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(serviceAccountJson),
                ProjectId = projectId
            });

            FirestoreDb = FirestoreDb.Create(projectId);
            Console.WriteLine("Firebase configurato da variabili d'ambiente");
        }

        private static void InitFromJsonFile(IConfiguration config)
        {
            var projectId = config["Firebase:ProjectId"];
            var jsonPath = config["Firebase:ServiceAccountPath"];

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"File di configurazione Firebase non trovato: {jsonPath}");
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(jsonPath),
                ProjectId = projectId
            });

            FirestoreDb = FirestoreDb.Create(projectId);
            Console.WriteLine("Firebase configurato da file locale");
        }
    }
}