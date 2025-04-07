using FarmTrackBE.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace FarmTrackBE.Services.BackgroundServices
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public NotificationBackgroundService(
            ILogger<NotificationBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Notification Background Service running check at: {time}", DateTimeOffset.Now);

                try
                {
                    await ProcessNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing notifications.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Notification Background Service is stopping.");
        }

        private async Task ProcessNotificationsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                var usersCollection = FirebaseInitializer.FirestoreDb.Collection("users");
                var userSnapshot = await usersCollection.GetSnapshotAsync();

                foreach (var userDoc in userSnapshot.Documents)
                {
                    string userUid = userDoc.Id;
                    _logger.LogInformation("Processing notifications for user: {userUid}", userUid);

                    await notificationService.CheckAndCreateDrugExpirationNotifications(userUid);
                }

                _logger.LogInformation("Notification check completed for all users");
            }
        }
    }
}
