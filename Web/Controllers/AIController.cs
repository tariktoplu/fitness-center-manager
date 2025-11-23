using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize]
public class AIController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AIController(IConfiguration configuration)
    {
        _configuration = configuration;

        // SSL Hatasını Atlatmak İçin (Linux/Rider Özel)
        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, cetChain, policyErrors) => true;

        _httpClient = new HttpClient(handler);
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new AIRequestViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> GeneratePlan(AIRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            // API Key Kontrolü
            string apiKey = _configuration["GoogleAI:ApiKey"] ?? string.Empty;
            
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("BURAYA_"))
            {
                model.AIResponse = "<span class='text-danger'>Hata: API Anahtarı bulunamadı.</span>";
                return View("Index", model);
            }

            // BMI Hesapla
            decimal heightInMeters = model.Height / 100m;
            decimal bmi = model.Weight / (heightInMeters * heightInMeters);
            model.BMIResult = $"Vücut Kitle İndeksiniz: {bmi:F2}";

            // Prompt Hazırla
            string prompt = $@"
                Sen bir spor hocasısın.
                Kullanıcı: Boy {model.Height} cm, Kilo {model.Weight} kg, Hedef: {model.Goal}.
                HTML formatında (sadece div, h3, p, ul, li etiketleri kullanarak) şunları yaz:
                1. Vücut analizi.
                2. Beslenme tavsiyesi (3 madde).
                3. Haftalık egzersiz planı.
                Cevap Türkçe olsun.";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            string jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // --- DEĞİŞİKLİK BURADA ---
            // 'gemini-1.5-flash' yerine 'gemini-pro' kullanıyoruz. En kararlı model budur.
            // Listende açıkça görünen 'gemini-2.0-flash' modelini kullanıyoruz:
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
            
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponseRoot>(responseString);
                
                string aiText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "Cevap üretilemedi.";
                
                aiText = aiText.Replace("```html", "").Replace("```", "");
                model.AIResponse = aiText;
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                model.AIResponse = $"<span class='text-danger'>API Hatası (Kod {response.StatusCode}): {errorContent}</span>";
            }
        }
        catch (Exception ex)
        {
            model.AIResponse = "Bağlantı hatası: " + ex.Message;
        }

        return View("Index", model);
    }
}

// Yardımcı Sınıflar (Aynı kalacak)
public class GeminiResponseRoot { [JsonPropertyName("candidates")] public List<Candidate>? Candidates { get; set; } }
public class Candidate { [JsonPropertyName("content")] public Content? Content { get; set; } }
public class Content { [JsonPropertyName("parts")] public List<Part>? Parts { get; set; } }
public class Part { [JsonPropertyName("text")] public string? Text { get; set; } }