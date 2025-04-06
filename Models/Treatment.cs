using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace FarmTrackBE.Models
{
    [FirestoreData]
    public class Treatment
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string AnimalId { get; set; }


        [FirestoreProperty]
        public DateTime Date { get; set; }

        [FirestoreProperty]
        public DateTime? EndDate { get; set; } // Data di fine trattamento (per trattamenti multipli)

        [FirestoreProperty]
        public string Type { get; set; } // Tipo di trattamento (es. vaccinazione, terapia, prevenzione)

        [FirestoreProperty]
        public string DrugId { get; set; } // ID del farmaco utilizzato

        [FirestoreProperty]
        public string DrugUsed { get; set; } // Nome del farmaco utilizzato

        [FirestoreProperty]
        public double Dosage { get; set; } // Dosaggio somministrato

        [FirestoreProperty]
        public string DosageUnit { get; set; } // Unità di misura del dosaggio (ml, mg, ecc.)

        [FirestoreProperty]
        public string AdministrationRoute { get; set; } // Via di somministrazione

        [FirestoreProperty]
        public string Veterinarian { get; set; } // Veterinario che ha eseguito il trattamento

        [FirestoreProperty]
        public string VeterinarianContact { get; set; } // Contatto del veterinario

        [FirestoreProperty]
        public string Diagnosis { get; set; } // Diagnosi o motivo del trattamento

        [FirestoreProperty]
        public string Notes { get; set; } // Note aggiuntive sul trattamento


        [FirestoreProperty]
        public string Outcome { get; set; } // Esito del trattamento

        [FirestoreProperty]
        public bool IsCompleted { get; set; } = false; // Indica se il trattamento è completato

        [FirestoreProperty]
        public List<TreatmentFollowUp> FollowUps { get; set; } = new List<TreatmentFollowUp>(); // Follow-up programmati

        [FirestoreProperty]
        public double Cost { get; set; } // Costo del trattamento


        [FirestoreProperty]
        public string Currency { get; set; } = "EUR"; // Valuta del costo

        [FirestoreProperty]
        public string UserUID { get; set; } // ID dell'utente proprietario

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Data di creazione del record



        public bool IsOngoing => !IsCompleted && EndDate.HasValue && EndDate.Value > DateTime.Now;


        public int DurationDays => EndDate.HasValue
            ? (EndDate.Value - Date).Days
            : (DateTime.Now - Date).Days;

        public string FormattedCost => $"{Cost:F2} {Currency}";

        // Metodo per aggiungere un follow-up
        public void AddFollowUp(DateTime scheduledDate, string description)
        {
            if (FollowUps == null)
                FollowUps = new List<TreatmentFollowUp>();

            FollowUps.Add(new TreatmentFollowUp
            {
                ScheduledDate = scheduledDate,
                Description = description,
                IsCompleted = false
            });
        }

        public void CompleteFollowUp(int index, string notes)
        {
            if (FollowUps != null && index >= 0 && index < FollowUps.Count)
            {
                FollowUps[index].IsCompleted = true;
                FollowUps[index].CompletionDate = DateTime.UtcNow;
                FollowUps[index].Notes = notes;
            }
        }

        public void Complete(string outcome)
        {
            IsCompleted = true;
            Outcome = outcome;
        }
    }

    [FirestoreData]
    public class TreatmentFollowUp
    {
        [FirestoreProperty]
        public DateTime ScheduledDate { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public bool IsCompleted { get; set; }

        [FirestoreProperty]
        public DateTime? CompletionDate { get; set; }

        [FirestoreProperty]
        public string Notes { get; set; }
    }
}
