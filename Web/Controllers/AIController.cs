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

        // --- SSL HATASI ÇÖZÜMÜ (LINUX/RIDER İÇİN) ---
        // Google API çağrılarında SSL hatası almamak için sertifika kontrolünü es geçiyoruz.
        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.ServerCertificateCustomValidationCallback = 
            (httpRequestMessage, cert, cetChain, policyErrors) => true;

        _httpClient = new HttpClient(handler);
    }

    // Sayfanın kendisini açan metod
    [HttpGet]
    public IActionResult Index()
    {
        return View(new AIRequestViewModel());
    }

    // --- VUE.JS İÇİN JSON API ---
    [HttpPost]
    [Route("AI/Analyze")] // URL'i sabitliyoruz: /AI/Analyze
    [IgnoreAntiforgeryToken] // Vue fetch işleminde token hatası almamak için
    public async Task<IActionResult> Analyze([FromBody] AIRequestViewModel model)
    {
        // Model boş gelirse
        if (model == null) 
            return BadRequest(new { success = false, message = "Veri alınamadı." });

        if (model.Height <= 0 || model.Weight <= 0)
            return BadRequest(new { success = false, message = "Lütfen geçerli boy ve kilo giriniz." });

        try
        {
            // API Key Kontrolü
            string apiKey = _configuration["GoogleAI:ApiKey"] ?? string.Empty;
            
            // Anahtar yoksa veya placeholder duruyorsa hata dön
            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("BURAYA_"))
                return BadRequest(new { success = false, message = "API Anahtarı bulunamadı." });

            // BMI Hesapla
            decimal heightInMeters = model.Height / 100m;
            decimal bmi = model.Weight / (heightInMeters * heightInMeters);
            string bmiResult = $"Vücut Kitle İndeksi: {bmi:F2}";

            // Prompt Hazırla
            string prompt = $@"
                Sen bir spor hocasısın. Kullanıcı: Boy {model.Height}, Kilo {model.Weight}, Hedef: {model.Goal}.
                Lütfen HTML formatında (sadece div, h3, p, ul, li etiketleri kullanarak, ```html yazmadan) şunları yaz:
                1. Vücut analizi.
                2. 3 beslenme önerisi.
                3. Haftalık egzersiz planı.
                Cevap Türkçe olsun.";

            // Google'a İstek Gövdesi
            var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
            string jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Model URL (gemini-2.0-flash senin hesabında aktifti)
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
            
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponseRoot>(responseString);
                
                string aiText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "Cevap yok.";
                
                // Temizlik
                aiText = aiText.Replace("```html", "").Replace("```", "");

                // BAŞARILI: JSON DÖNÜYORUZ
                return Ok(new { success = true, bmi = bmiResult, htmlContent = aiText });
            }
            
            // Google Hata Döndüyse
            string errorDetails = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, new { success = false, message = "Google API Hatası: " + response.StatusCode });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Sunucu Hatası: " + ex.Message });
        }
    }
}

// --- Yardımcı Sınıflar ---
public class GeminiResponseRoot { [JsonPropertyName("candidates")] public List<Candidate>? Candidates { get; set; } }
public class Candidate { [JsonPropertyName("content")] public Content? Content { get; set; } }
public class Content { [JsonPropertyName("parts")] public List<Part>? Parts { get; set; } }
public class Part { [JsonPropertyName("text")] public string? Text { get; set; } }