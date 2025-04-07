using FarmTrackBE.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FarmTrackBE.Services
{
    public class NotificationService
    {
        private readonly CollectionReference _notificationsCollection;

        public NotificationService()
        {
            _notificationsCollection = FirebaseInitializer.FirestoreDb.Collection("notifications");
        }

        public async Task<List<Notification>> GetAllAsync(string userUid)
        {
            var snapshot = await _notificationsCollection
                .WhereEqualTo("UserUID", userUid)
                .OrderByDescending("CreatedAt")
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<Notification>())
                .ToList();
        }

        public async Task<List<Notification>> GetUnreadAsync(string userUid)
        {
            var snapshot = await _notificationsCollection
                .WhereEqualTo("UserUID", userUid)
                .WhereEqualTo("IsRead", false)
                .OrderByDescending("CreatedAt")
                .GetSnapshotAsync();

            return snapshot.Documents
                .Select(doc => doc.ConvertTo<Notification>())
                .ToList();
        }

        public async Task<Notification> GetByIdAsync(string id, string userUid)
        {
            var docRef = _notificationsCollection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            var notification = snapshot.ConvertTo<Notification>();
            if (notification.UserUID != userUid)
                return null;

            return notification;
        }

        public async Task AddAsync(Notification notification)
        {
            await _notificationsCollection.AddAsync(notification);
        }

        public async Task MarkAsReadAsync(string id, string userUid)
        {
            var notification = await GetByIdAsync(id, userUid);
            if (notification != null)
            {
                await _notificationsCollection.Document(id).UpdateAsync("IsRead", true);
            }
        }

        public async Task MarkAllAsReadAsync(string userUid)
        {
            var batch = FirebaseInitializer.FirestoreDb.StartBatch();
            var unreadNotifications = await GetUnreadAsync(userUid);

            foreach (var notification in unreadNotifications)
            {
                var docRef = _notificationsCollection.Document(notification.Id);
                batch.Update(docRef, "IsRead", true);
            }

            await batch.CommitAsync();
        }

        public async Task DeleteAsync(string id, string userUid)
        {
            var notification = await GetByIdAsync(id, userUid);
            if (notification != null)
            {
                await _notificationsCollection.Document(id).DeleteAsync();
            }
        }

        public async Task CheckAndCreateDrugExpirationNotifications(string userUid)
        {
            var drugsCollection = FirebaseInitializer.FirestoreDb.Collection("drugs");
            var snapshot = await drugsCollection
                .WhereEqualTo("UserUID", userUid)
                .GetSnapshotAsync();

            var drugs = snapshot.Documents
                .Select(doc => doc.ConvertTo<Drug>())
                .ToList();

            foreach (var drug in drugs)
            {
                if (drug.IsNearExpiration && !drug.IsExpired)
                {
                    var existingNotifications = await _notificationsCollection
                        .WhereEqualTo("UserUID", userUid)
                        .WhereEqualTo("Type", "DrugExpiration")
                        .WhereEqualTo("RelatedItemId", drug.Id)
                        .GetSnapshotAsync();

                    if (existingNotifications.Count == 0)
                    {
                        var notification = new Notification
                        {
                            Title = "Farmaco in scadenza",
                            Message = $"Il farmaco {drug.Name} scadrà tra {drug.DaysUntilExpiration} giorni ({drug.ExpirationDate.ToShortDateString()}).",
                            Type = "DrugExpiration",
                            RelatedItemId = drug.Id,
                            UserUID = userUid
                        };

                        await AddAsync(notification);
                    }
                }

                if (drug.IsLowStock)
                {
                    var existingNotifications = await _notificationsCollection
                        .WhereEqualTo("UserUID", userUid)
                        .WhereEqualTo("Type", "LowStock")
                        .WhereEqualTo("RelatedItemId", drug.Id)
                        .WhereGreaterThan("CreatedAt", DateTime.UtcNow.AddDays(-7)) // Evita notifiche duplicate entro 7 giorni
                        .GetSnapshotAsync();

                    if (existingNotifications.Count == 0)
                    {
                        var notification = new Notification
                        {
                            Title = "Scorte in esaurimento",
                            Message = $"Il farmaco {drug.Name} ha scorte basse (Quantità: {drug.Quantity}, Minimo: {drug.MinimumStockLevel}).",
                            Type = "LowStock",
                            RelatedItemId = drug.Id,
                            UserUID = userUid
                        };

                        await AddAsync(notification);
                    }
                }
            }
        }
    }
}
