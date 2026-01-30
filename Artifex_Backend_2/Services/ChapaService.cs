using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Artifex_Backend_2.DTOs;

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
            _secretKey = config["Chapa:SecretKey"];
            _httpClient.BaseAddress = new Uri(config["Chapa:BaseUrl"]);
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
                // Replace with your actual callback URL (for frontend redirection)
                return_url = "http://localhost:5173/payment/success"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("transaction/initialize", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Chapa Error: {responseString}");

            var result = JsonSerializer.Deserialize<ChapaResponseDto>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result.Data.Checkout_Url;
        }

        public async Task<bool> VerifyTransaction(string txRef)
        {
            var response = await _httpClient.GetAsync($"transaction/verify/{txRef}");
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return false;

            // Simple check. In production, deserialize and check data.status == "success"
            return responseString.Contains("success");
        }
    }
}