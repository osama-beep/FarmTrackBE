using FarmTrackBE.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FarmTrackBE.Services
{
    public class AnimalService
    {
        private readonly FirestoreDb _firestoreDb;
        private const string Collection = "animals";

        public AnimalService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb ?? throw new ArgumentNullException(nameof(firestoreDb));
        }

        public async Task<List<Animal>> GetAllAsync(string uid)
        {
            try
            {
                var snapshot = await _firestoreDb.Collection(Collection)
                    .WhereEqualTo("UserUID", uid)
                    .GetSnapshotAsync();

                return snapshot.Documents
                    .Select(doc =>
                    {
                        var animal = doc.ConvertTo<Animal>();
                        animal.Id = doc.Id;
                        return animal;
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero degli animali: {ex.Message}");
                throw;
            }
        }

        public async Task<Animal> GetByIdAsync(string id, string uid)
        {
            try
            {
                var docRef = _firestoreDb.Collection(Collection).Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                    return null;

                var animal = snapshot.ConvertTo<Animal>();

                if (animal.UserUID != uid)
                    return null;

                animal.Id = snapshot.Id;
                return animal;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel recupero dell'animale: {ex.Message}");
                throw;
            }
        }

        public async Task AddAsync(Animal animal)
        {
            try
            {
                if (animal.TotalAgeInMonths <= 0 && (animal.AgeYears > 0 || animal.AgeMonths > 0))
                {

                }
                else if (animal.TotalAgeInMonths > 0 && (animal.AgeYears == 0 && animal.AgeMonths == 0))
                {

                    animal.SetAgeFromTotalMonths(animal.TotalAgeInMonths);
                }

                var doc = _firestoreDb.Collection(Collection).Document();
                animal.Id = doc.Id;
                await doc.SetAsync(animal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiunta di un animale: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAsync(string id, Animal animal)
        {
            try
            {
                if (animal.TotalAgeInMonths > 0 && (animal.AgeYears == 0 && animal.AgeMonths == 0))
                {

                    animal.SetAgeFromTotalMonths(animal.TotalAgeInMonths);
                }

                await _firestoreDb.Collection(Collection)
                    .Document(id)
                    .SetAsync(animal, SetOptions.Overwrite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento dell'animale: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                await _firestoreDb.Collection(Collection)
                    .Document(id)
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'eliminazione dell'animale: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateHealthStatusAsync(string id, string newStatus, string uid)
        {
            try
            {
                var animal = await GetByIdAsync(id, uid);
                if (animal == null)
                    throw new Exception("Animale non trovato o non autorizzato");

                animal.HealthStatus = newStatus;
                await UpdateAsync(id, animal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento dello stato di salute: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateWeightAsync(string id, double newWeight, string uid)
        {
            try
            {
                var animal = await GetByIdAsync(id, uid);
                if (animal == null)
                    throw new Exception("Animale non trovato o non autorizzato");

                animal.Weight = newWeight;
                await UpdateAsync(id, animal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento del peso: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAgeAsync(string id, int years, int months, string uid)
        {
            try
            {
                var animal = await GetByIdAsync(id, uid);
                if (animal == null)
                    throw new Exception("Animale non trovato o non autorizzato");

                animal.AgeYears = years;
                animal.AgeMonths = months;
                await UpdateAsync(id, animal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento dell'età: {ex.Message}");
                throw;
            }
        }
    }
}
