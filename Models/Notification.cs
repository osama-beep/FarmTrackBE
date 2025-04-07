using Google.Cloud.Firestore;
using System;

namespace FarmTrackBE.Models
{
    [FirestoreData]
    public class Notification
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Title { get; set; }

        [FirestoreProperty]
        public string Message { get; set; }

        [FirestoreProperty]
        public string Type { get; set; }

        [FirestoreProperty]
        public string RelatedItemId { get; set; } 

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [FirestoreProperty]
        public bool IsRead { get; set; } = false;

        [FirestoreProperty]
        public string UserUID { get; set; }
    }
}
