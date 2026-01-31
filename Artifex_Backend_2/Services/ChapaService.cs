using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Artifex_Backend_2.DTOs;
using Microsoft.Extensions.Configuration;

namespace Artifex_Backend_2.Services
{
    public interface IChapaService
    {
        Task<string> InitializeTransaction(string txRef, decimal amount, string email, string fName, string lName);
        Task<bool> VerifyTransaction(string txRef);
    }

    public class ChapaService : IChapaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public ChapaService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;

            // 1. Get Config
            _secretKey = config["Chapa:SecretKey"] ?? config["Chapa__SecretKey"];
            var baseUrl = config["Chapa:BaseUrl"] ?? config["Chapa__BaseUrl"];

            if (string.IsNullOrEmpty(_secretKey) || string.IsNullOrEmpty(baseUrl))
            {
                throw new Exception("❌ Chapa Config Missing! Check Render Environment Variables.");
            }

            // 2. THE FIX: Force the URL to end with '/'
            // If this is missing, HttpClient deletes the '/v1' part automatically.
            if (!baseUrl.EndsWith("/"))
            {
                baseUrl += "/";
            }

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _secretKey);
        }

        public async Task<string> InitializeTransaction(string txRef, decimal amount, string email, string fName, string lName)
        {
            var payload = new
            {
                amount = amount,
                currency = "ETB",
                email = email,
                first_name = fName,
                last_name = lName,
                tx_ref = txRef,
                // Ensure this points to your LIVE frontend
                return_url = "https://localhost:5173/payment/success"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Now this will correctly combine to: https://api.chapa.co/v1/transaction/initialize
            var response = await _httpClient.PostAsync("transaction/initialize", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log the exact error from Chapa so we can see it
                throw new Exception($"Chapa Error ({response.StatusCode}): {responseString}");
            }

            var result = JsonSerializer.Deserialize<ChapaResponseDto>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Data?.Checkout_Url ?? throw new Exception("Chapa did not return a checkout URL.");
        }

        public async Task<bool> VerifyTransaction(string txRef)
        {
            var response = await _httpClient.GetAsync($"transaction/verify/{txRef}");
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return false;

            return responseString.Contains("success");
        }
    }
}