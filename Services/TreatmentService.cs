using FarmTrackBE.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FarmTrackBE.Services
{
    public class TreatmentService
    {
        private readonly FirestoreDb _db;
        private const string Collection = "treatments";

        public TreatmentService(FirestoreDb firestoreDb)
        {
            _db = firestoreDb;
        }

        public async Task<List<Treatment>> GetAllAsync(string uid)
        {
            try
            {
                var snapshot = await _db.Collection(Collection).WhereEqualTo("UserUID", uid).GetSnapshotAsync();
                return snapshot.Documents.Select(doc =>
                {
                    var treatment = doc.ConvertTo<Treatment>();
                    treatment.Id = doc.Id;
                    return treatment;
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero dei trattamenti: {ex.Message}");
                throw;
            }
        }

        public async Task<Treatment> GetByIdAsync(string id, string uid)
        {
            try
            {
                var docRef = _db.Collection(Collection).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return null;

                var treatment = snapshot.ConvertTo<Treatment>();

                if (treatment.UserUID != uid)
                    return null;

                treatment.Id = snapshot.Id;
                return treatment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero del trattamento: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Treatment>> GetByAnimalIdAsync(string animalId, string uid)
        {
            try
            {
                var snapshot = await _db.Collection(Collection)
                    .WhereEqualTo("AnimalId", animalId)
                    .WhereEqualTo("UserUID", uid)
                    .GetSnapshotAsync();

                return snapshot.Documents.Select(doc =>
                {
                    var treatment = doc.ConvertTo<Treatment>();
                    treatment.Id = doc.Id;
                    return treatment;
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero dei trattamenti per animale: {ex.Message}");
                throw;
            }
        }

        public async Task AddAsync(Treatment treatment)
        {
            try
            {
                var doc = _db.Collection(Collection).Document();
                treatment.Id = doc.Id;
                await doc.SetAsync(treatment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiunta del trattamento: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(string id, Treatment treatment)
        {
            try
            {
                await _db.Collection(Collection).Document(id).SetAsync(treatment, SetOptions.Overwrite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento del trattamento: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                await _db.Collection(Collection).Document(id).DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'eliminazione del trattamento: {ex.Message}");
                throw;
            }
        }

        public async Task CompleteAsync(string id, string outcome, string uid)
        {
            try
            {
                var treatment = await GetByIdAsync(id, uid);
                if (treatment == null)
                    throw new Exception("Trattamento non trovato o non autorizzato");

                treatment.Outcome = outcome;
                treatment.CompletionDate = DateTime.UtcNow;
                treatment.IsCompleted = true;

                await UpdateAsync(id, treatment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel completamento del trattamento: {ex.Message}");
                throw;
            }
        }

        public async Task AddFollowUpAsync(string id, DateTime scheduledDate, string description, string uid)
        {
            try
            {
                var treatment = await GetByIdAsync(id, uid);
                if (treatment == null)
                    throw new Exception("Trattamento non trovato o non autorizzato");

                treatment.AddFollowUp(scheduledDate, description);

                await UpdateAsync(id, treatment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiunta del follow-up: {ex.Message}");
                throw;
            }
        }

        public async Task CompleteFollowUpAsync(string id, int followUpIndex, string notes, string uid)
        {
            try
            {
                var treatment = await GetByIdAsync(id, uid);
                if (treatment == null)
                    throw new Exception("Trattamento non trovato o non autorizzato");

                treatment.CompleteFollowUp(followUpIndex, notes);

                await UpdateAsync(id, treatment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel completamento del follow-up: {ex.Message}");
                throw;
            }
        }
    }
}
