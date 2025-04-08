using Google.Cloud.Firestore;

namespace FarmTrackBE.Models
{
    [FirestoreData]
    public class Animal
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Breed { get; set; }

        [FirestoreProperty]
        public string Species { get; set; }

        [FirestoreProperty]
        public int AgeYears { get; set; }

        [FirestoreProperty]
        public int AgeMonths { get; set; }

        [FirestoreProperty]
        public double Weight { get; set; }

        [FirestoreProperty]
        public string HealthStatus { get; set; }

        [FirestoreProperty]
        public string UserUID { get; set; }

        [FirestoreProperty]
        public string ImageUrl { get; set; }

        public int TotalAgeInMonths => AgeYears * 12 + AgeMonths;

        public string FormattedAge
        {
            get
            {
                if (AgeYears == 0)
                {
                    return $"{AgeMonths} {(AgeMonths == 1 ? "mese" : "mesi")}";
                }
                else if (AgeMonths == 0)
                {
                    return $"{AgeYears} {(AgeYears == 1 ? "anno" : "anni")}";
                }
                else
                {
                    return $"{AgeYears} {(AgeYears == 1 ? "anno" : "anni")} e {AgeMonths} {(AgeMonths == 1 ? "mese" : "mesi")}";
                }
            }
        }

        public void SetAgeFromTotalMonths(int totalMonths)
        {
            AgeYears = totalMonths / 12;
            AgeMonths = totalMonths % 12;
        }
    }
}
