using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ScreenAgent.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsService _settingsService;
        private const string API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string DEFAULT_MODEL = "gemini-2.0-flash-exp";

        public GeminiService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
        }

        public async Task<string> GetResponseFromImageBatchAndTextAsync(List<byte[]> imageBytesList, string prompt)
        {
            var apiKey = _settingsService.GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("API Key not set");

            // 從設定中取得模型名稱
            string modelName = _settingsService.GetGeminiModel();
            if (string.IsNullOrEmpty(modelName))
            {
                modelName = DEFAULT_MODEL;
            }

            // 從設定中取得 System Prompt
            string systemPrompt = _settingsService.GetSystemPrompt();

            // 準備多模態請求
            var requestUrl = $"{API_BASE_URL}{modelName}:generateContent?key={apiKey}";
            
            // 構建請求內容陣列，包含提示文字部分
            var parts = new List<object>();
            
            // 添加文字提示
            parts.Add(new { text = prompt });
            
            // 添加所有圖片 (如果有的話)
            if (imageBytesList != null && imageBytesList.Count > 0)
            {
                foreach (var imageBytes in imageBytesList)
                {
                    var base64Image = Convert.ToBase64String(imageBytes);
                    parts.Add(new
                    {
                        inlineData = new
                        {
                            mimeType = "image/jpeg",
                            data = base64Image
                        }
                    });
                }
            }
            
            // 構建最終請求體 (符合最新 Gemini API 文檔格式)
            var requestJson = @"
            {
                ""system_instruction"": {
                    ""parts"": [
                        {
                            ""text"": """ + systemPrompt + @"""
                        }
                    ]
                },
                ""contents"": [
                    {
                        ""parts"": " + JsonSerializer.Serialize(parts) + @"
                    }
                ],
                ""tools"": [
                    {
                        ""google_search"": {}
                    }
                ]
            }";

            try
            {
                Console.WriteLine($"發送請求到 Gemini API... (模型: {modelName}, 包含 {(imageBytesList?.Count ?? 0)} 張圖片)");
                
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Gemini API 錯誤: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    Console.WriteLine($"響應內容: {responseContent}");
                    return $"API 錯誤 {(int)response.StatusCode}: {responseContent}";
                }
                
                Console.WriteLine("成功獲取響應!");
                var jsonResponse = JsonDocument.Parse(responseContent);
                
                try
                {
                    // 解析回應
                    return jsonResponse.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "無回應";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"解析響應時發生錯誤: {ex.Message}");
                    Console.WriteLine($"原始響應: {responseContent}");
                    return "解析 API 響應時發生錯誤";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Gemini API: {ex.Message}");
                return $"無法處理請求: {ex.Message}";
            }
        }
        
        // 保留原來的單圖像方法，轉調用新的多圖像方法
        public async Task<string> GetResponseFromImageAndTextAsync(byte[] imageBytes, string prompt)
        {
            return await GetResponseFromImageBatchAndTextAsync(new List<byte[]> { imageBytes }, prompt);
        }
    }
}