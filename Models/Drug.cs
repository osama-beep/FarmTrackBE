using Google.Cloud.Firestore;
using System;

namespace FarmTrackBE.Models
{
    [FirestoreData]
    public class Drug
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Type { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public int Quantity { get; set; }


        [FirestoreProperty]
        public double Price { get; set; } // Prezzo per unità

        [FirestoreProperty]
        public string Currency { get; set; } = "EUR"; // Valuta (default EUR)

        [FirestoreProperty]
        public DateTime ExpirationDate { get; set; } // Data di scadenza

        [FirestoreProperty]
        public string AdministrationRoute { get; set; } // Via di somministrazione (es. orale, iniettabile)

        [FirestoreProperty]
        public int MinimumStockLevel { get; set; } = 5; // Livello minimo di scorta per avvisi

        [FirestoreProperty]
        public DateTime PurchaseDate { get; set; } // Data di acquisto

        [FirestoreProperty]
        public string UserUID { get; set; }

        public bool IsLowStock => Quantity <= MinimumStockLevel;

        public bool IsExpired => ExpirationDate.Date < DateTime.Today;

        public double TotalValue => Math.Round(Price * Quantity, 2);

        public string FormattedPrice => $"{Price:F2} {Currency}";

        public int DaysUntilExpiration => ExpirationDate > DateTime.Now
            ? (ExpirationDate - DateTime.Now).Days
            : 0;

        // Metodo per verificare se il farmaco è in scadenza (entro 30 giorni)
        public bool IsNearExpiration => !IsExpired && DaysUntilExpiration <= 30;

        public string FormattedTotalValue => $"{TotalValue:F2} {Currency}";
    }
}
