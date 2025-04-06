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

                // Get the service account path
                var serviceAccountPath = config["Firebase:ServiceAccountPath"];
                if (string.IsNullOrEmpty(serviceAccountPath))
                {
                    // Try to find it in the default location
                    serviceAccountPath = Path.Combine(Directory.GetCurrentDirectory(), "secrets", "firebase-service-account.json");
                    _logger?.LogInformation($"ServiceAccountPath not specified, trying default: {serviceAccountPath}");
                }

                // Make sure the path is absolute
                if (!Path.IsPathRooted(serviceAccountPath))
                {
                    serviceAccountPath = Path.Combine(Directory.GetCurrentDirectory(), serviceAccountPath);
                }

                _logger?.LogInformation($"Using service account file: {serviceAccountPath}");

                // Check if the file exists
                if (!File.Exists(serviceAccountPath))
                {
                    _logger?.LogError($"Service account file not found: {serviceAccountPath}");
                    throw new FileNotFoundException($"Firebase service account file not found: {serviceAccountPath}");
                }

                // Read the file content to verify it's valid
                string jsonContent;
                try
                {
                    jsonContent = File.ReadAllText(serviceAccountPath);
                    _logger?.LogInformation($"Successfully read service account file ({jsonContent.Length} bytes)");

                    // Verify it's valid JSON
                    var jsonDoc = JsonDocument.Parse(jsonContent);
                    _logger?.LogInformation("Service account file contains valid JSON");

                    // Verify it has required fields
                    if (!jsonDoc.RootElement.TryGetProperty("type", out var typeElement) ||
                        typeElement.GetString() != "service_account")
                    {
                        _logger?.LogError("Service account file is missing 'type' field or is not a service account");
                        throw new InvalidOperationException("Invalid service account file: missing or incorrect 'type' field");
                    }

                    if (!jsonDoc.RootElement.TryGetProperty("private_key", out var _))
                    {
                        _logger?.LogError("Service account file is missing 'private_key' field");
                        throw new InvalidOperationException("Invalid service account file: missing 'private_key' field");
                    }

                    if (!jsonDoc.RootElement.TryGetProperty("client_email", out var _))
                    {
                        _logger?.LogError("Service account file is missing 'client_email' field");
                        throw new InvalidOperationException("Invalid service account file: missing 'client_email' field");
                    }
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    _logger?.LogError(ex, "Failed to read or parse service account file");
                    throw new InvalidOperationException("Failed to read or parse service account file", ex);
                }

                // Get the project ID
                string projectId = config["Firebase:ProjectId"];

                if (string.IsNullOrEmpty(projectId))
                {
                    // Try to extract it from the service account file
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(jsonContent);
                        if (jsonDoc.RootElement.TryGetProperty("project_id", out var projectIdElement))
                        {
                            projectId = projectIdElement.GetString();
                            _logger?.LogInformation($"Extracted project ID from service account file: {projectId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to extract project ID from service account file");
                    }
                }

                if (string.IsNullOrEmpty(projectId))
                {
                    _logger?.LogError("Project ID not found in configuration or service account file");
                    throw new InvalidOperationException("Firebase project ID not specified");
                }

                // Initialize Firebase with explicit credentials
                _logger?.LogInformation($"Initializing Firebase with project ID: {projectId}");

                try
                {
                    // Set environment variable for Google Application Default Credentials
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", serviceAccountPath);
                    _logger?.LogInformation($"Set GOOGLE_APPLICATION_CREDENTIALS to {serviceAccountPath}");

                    // Create credential from file
                    var credential = GoogleCredential.FromFile(serviceAccountPath);
                    _logger?.LogInformation("Successfully created GoogleCredential from file");

                    // Create Firebase app
                    FirebaseApp = FirebaseAdmin.FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential,
                        ProjectId = projectId
                    });
                    _logger?.LogInformation("Successfully created FirebaseApp");

                    // Initialize Firestore
                    _logger?.LogInformation("Initializing Firestore");
                    FirestoreDb = FirestoreDb.Create(projectId);
                    _logger?.LogInformation("Successfully created FirestoreDb");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize Firebase with service account file");
                    throw;
                }

                _logger?.LogInformation("Firebase initialization successful");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Errore critico durante l'inizializzazione di Firebase");
                throw new FirebaseInitializationException("Fallimento inizializzazione Firebase", ex);
            }
        }
    }

    public class FirebaseInitializationException : Exception
    {
        public FirebaseInitializationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
