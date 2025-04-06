using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System;
using System.IO;
using System.Text;

namespace FarmTrackBE.Services
{
    public static class FirebaseInitializer
    {
        public static FirestoreDb FirestoreDb { get; private set; }
        public static string ProjectId { get; private set; } = "farmtrackbe";

        public static void Initialize()
        {
            try
            {
                string jsonCredentials = @"{
                    ""type"": ""service_account"",
                    ""project_id"": ""farmtrackbe"",
                    ""private_key_id"": ""f7974bb3a709889e7ab7d826f8fe0335920ce567"",
                    ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC62hhDOqVLgpS+\nkREm8IsxU50Et7f6E9VGSSjyJXu3QyWqe2V0GeSK1QGEqtTdvQjDeXwhXRbL5HCe\n5milrT4eXv606A3ejtcJbAEQgveu3ZwBPE0dR0jDAUk2uea4Pr0E93XHlJjeTqnA\n2FbZQfa/O4N8q2VLoR2asDh6QXQwwXcO52LVzkr+l2zYhP1pEe9MSviOPvVElLe9\nBS5tRaI9tCtvVzzhcVtLH4/DOj673ltv/Y2j9zJ2WDDy47AEmr04NnI4C7NB/H8E\ni6w7QBeAqzTCMewoPLPdLpAPijxk+CKFysWM1pVary9lYJMsBd0v9bQ/BphecJWL\n4W4it0JTAgMBAAECggEARA9P2iEg902zn2xYyo3ArbFulrrSrSkPPOBGJVmI5Dp1\nnFEBJdaOnBE1Ud0l1zTVXdpA4Cg3twHZEdAMWxis8UQZ08xYzPHLmvd3tct2q3+2\nJO8RibIe60TrJZ92TjMwB4CZqp5SRltx0rgafl1cItDrNnx505/2mBNLJyDzURRv\n1Gmv8UzvIetacoZSkC1LlJgBP6MG5Bz79pRDSXhSmH7i2D5yLMkVE5jSOK5JLLmB\noP8pYJssrn8glUtPKFw1qPIbLvdBGN8+gD0dAl8T4SK384HblXPDuZ10zx0mFUmp\nGq7oqYeFUYsA+TAVRiz+TjMTSrZEXtqcI8p/aKOh3QKBgQDa8SBcYvj2eSWvV1EI\n4qtDytbJA22J5EE3ilRgN7E02mC1asOV/C8CZWXfR6g4yEhw0tf/mfceFcWw8J1w\nAGcSSbTcLy/QUXL55GNLx+2ZuUCeNb6Wnhvx+oA3dLWUVqlS3IjWfPXeppRE7nJS\nOXiGgb7znpbfDJ5AyXHzocRyPQKBgQDaen4J4sGt0oH6Wr5VHIouE/XEOu0Peubc\nD1H37yiuhG50irthU2+HgJfMQLCShc/a2tK6jUdVK5pKTFNL4ttRLhz+JguSCSaR\n++X7Nbz9UXdpxI49aZOdo6zS+KJ8sZh0YoFY1Zcl6V+iuUetSOZKKzcQRBtVzz1w\nHiK5X0efzwKBgQCrMjJ4qxc7Or2B3ofJp9v9NWU/ZsPHN8jbEfoqBkI1LrDCCoqH\nA9sKR5khvxlF1S33spYdEhoN1z5uvaaNhnMR9LpMFUWQ4a9CwRf3kIw0UIu7ahDu\nGxGE47hQJJ07MYxS84i4Fpv2jlCPmdegfbnFizxxqEcWf/padGn69Dn0jQKBgFIx\n41k0xkju+ZeGrDS5GANd3wiEYsuAIXqJCP2OLG/7wtJ1nyluincgGfvuGoAFd72O\ntdkZbczNKq7pKC1Um85e2umCxreaWbhyXIO2I/PpwlM4b4sLPU4BVfPJNthSuFiQ\n9rjFIqhQtHoz2pOG6ZzdxUmJVf9iiGq167px7jBhAoGAVgJFVjG+56RF2vNieTEx\npEwYOAYqdY+gyNitESbD4Z1xpQcU4dpHm/vfjq/yf0S3xZoF3TTwaFWZOAtnQjKm\nELAXWNwN3w4lrzBLGaKQylxMtbvBBtq4QJs7Yz34FjyNp2MshcrPDA2eC/yYzqKn\nxuum9GwFhDgJ8I8Al1XgerA=\n-----END PRIVATE KEY-----\n"",
                    ""client_email"": ""firebase-adminsdk-fbsvc@farmtrackbe.iam.gserviceaccount.com"",
                    ""client_id"": ""118124489937716623652"",
                    ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
                    ""token_uri"": ""https://oauth2.googleapis.com/token"",
                    ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
                    ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-fbsvc%40farmtrackbe.iam.gserviceaccount.com"",
                    ""universe_domain"": ""googleapis.com""
                }";

                var credential = GoogleCredential.FromJson(jsonCredentials);

                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential,
                        ProjectId = ProjectId
                    });

                    Console.WriteLine("Firebase Admin SDK inizializzato con successo");
                }
                else
                {
                    Console.WriteLine("Firebase Admin SDK già inizializzato");
                }

                FirestoreDbBuilder builder = new FirestoreDbBuilder
                {
                    ProjectId = ProjectId,
                    Credential = credential
                };

                FirestoreDb = builder.Build();
                Console.WriteLine($"Firestore inizializzato con successo per il progetto: {ProjectId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRORE durante l'inizializzazione di Firebase: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                throw;
            }
        }
    }
}
