using FarmTrackBE.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FarmTrackBE.Services
{
    public class DrugService
    {
        private readonly FirestoreDb _db;
        private const string Collection = "drugs";

        public DrugService(FirestoreDb firestoreDb)
        {
            _db = firestoreDb;
        }

        public async Task<List<Drug>> GetAllAsync(string uid)
        {
            try
            {
                var snapshot = await _db.Collection(Collection).WhereEqualTo("UserUID", uid).GetSnapshotAsync();
                return snapshot.Documents.Select(doc =>
                {
                    var drug = doc.ConvertTo<Drug>();
                    drug.Id = doc.Id;
                    return drug;
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero dei farmaci: {ex.Message}");
                throw;
            }
        }

        public async Task<Drug> GetByIdAsync(string id, string uid)
        {
            try
            {
                var docRef = _db.Collection(Collection).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return null;

                var drug = snapshot.ConvertTo<Drug>();

                if (drug.UserUID != uid)
                    return null;

                drug.Id = snapshot.Id;
                return drug;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero del farmaco: {ex.Message}");
                throw;
            }
        }

        public async Task AddAsync(Drug drug)
        {
            try
            {
                var doc = _db.Collection(Collection).Document();
                drug.Id = doc.Id;
                await doc.SetAsync(drug);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiunta del farmaco: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(string id, Drug drug)
        {
            try
            {
                await _db.Collection(Collection).Document(id).SetAsync(drug, SetOptions.Overwrite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento del farmaco: {ex.Message}");
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
                Console.WriteLine($"Errore nell'eliminazione del farmaco: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Drug>> GetLowStockAsync(string uid)
        {
            try
            {
                var allDrugs = await GetAllAsync(uid);
                return allDrugs.Where(d => d.IsLowStock).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero dei farmaci con scorte basse: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Drug>> GetExpiringAsync(string uid, int daysThreshold = 30)
        {
            try
            {
                var allDrugs = await GetAllAsync(uid);
                return allDrugs.Where(d => !d.IsExpired && d.DaysUntilExpiration <= daysThreshold).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero dei farmaci in scadenza: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Drug>> GetExpiredAsync(string uid)
        {
            try
            {
                var allDrugs = await GetAllAsync(uid);
                return allDrugs.Where(d => d.IsExpired).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero dei farmaci scaduti: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateQuantityAsync(string id, int newQuantity, string uid)
        {
            try
            {
                var drug = await GetByIdAsync(id, uid);
                if (drug == null)
                    throw new Exception("Farmaco non trovato o non autorizzato");

                drug.Quantity = newQuantity;
                await UpdateAsync(id, drug);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento della quantità: {ex.Message}");
                throw;
            }
        }
    }
}
