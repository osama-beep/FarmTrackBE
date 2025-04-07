using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FarmTrackBE.Services
{
    public class ImageKitUploadService
    {
        private readonly string _uploadUrl = "https://upload.imagekit.io/api/v1/files/upload";
        private readonly string _publicKey;
        private readonly string _privateKey;
        private readonly string _urlEndpoint;

        public ImageKitUploadService(IConfiguration configuration)
        {
            _publicKey = configuration["ImageKit:PublicKey"];
            _privateKey = configuration["ImageKit:PrivateKey"];
            _urlEndpoint = configuration["ImageKit:UrlEndpoint"];
        }

        public async Task<string> UploadImageAsync(string userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File non valido");

            var client = new RestClient(_uploadUrl);
            var request = new RestRequest();
            request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_privateKey}:")));

            request.AddParameter("fileName", $"profile_{userId}_{Guid.NewGuid()}");
            request.AddParameter("useUniqueFileName", "true");
            request.AddParameter("folder", $"/profile_images/{userId}");

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                request.AddFile("file", memoryStream.ToArray(), file.FileName, file.ContentType);
            }

            var response = await client.PostAsync(request);

            if (!response.IsSuccessful)
                throw new Exception("Errore durante l'upload dell'immagine: " + response.Content);

            dynamic result = System.Text.Json.JsonDocument.Parse(response.Content!).RootElement;
            string url = result.GetProperty("url").GetString()!;
            return url;
        }
    }
}
