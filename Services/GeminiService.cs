using System.Net.Http;
using System.Text;
using System.Text.Json;
using ScreenAgent.Models;

namespace ScreenAgent.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsService _settingsService;
        private const string API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string DEFAULT_MODEL = "gemini-2.0-flash-exp";
        private List<Content> _conversation;

        public GeminiService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
            _conversation = new List<Content>();
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

            // 清空對話歷史，因為包含圖片的請求不適合保存對話歷史
            _conversation.Clear();

            // 準備多模態請求
            var requestUrl = $"{API_BASE_URL}{modelName}:generateContent?key={apiKey}";
            
            // 構建請求內容陣列，包含提示文字部分
            var parts = new List<Part>();
            
            // 添加文字提示
            parts.Add(new Part { Text = prompt });
            
            // 添加所有圖片 (如果有的話)
            if (imageBytesList != null && imageBytesList.Count > 0)
            {
                foreach (var imageBytes in imageBytesList)
                {
                    var base64Image = Convert.ToBase64String(imageBytes);
                    parts.Add(new Part { 
                        InlineData = new InlineData
                        {
                            MimeType = "image/jpeg",
                            Data = base64Image
                        }
                    });
                }
            }
            
            // 添加用戶訊息到對話歷史
            _conversation.Add(new Content
            {
                Role = "user",
                Parts = parts
            });

            // 構建最終請求體
            var request = new GeminiRequest
            {
                SystemInstruction = new SystemInstruction
                {
                    Parts = new List<Part> { new Part { Text = systemPrompt } }
                },
                Contents = _conversation,
                Tools = GetTools(hasImages: imageBytesList != null && imageBytesList.Count > 0)
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return $"API 錯誤 {(int)response.StatusCode}: {responseContent}";
                }

                // 反序列化回應
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                
                // 檢查是否有回應
                if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
                {
                    return "無回應";
                }

                var candidate = geminiResponse.Candidates[0];
                var candidateContent = candidate.Content;

                // 將回應添加到對話歷史
                _conversation.Add(candidateContent);

                // 檢查是否為函數調用回應
                if (candidateContent.Parts.Count > 0 && candidateContent.Parts[0].FunctionCall != null)
                {
                    // 處理函數調用
                    return await HandleFunctionCallAsync(candidateContent.Parts[0].FunctionCall, apiKey, modelName, requestUrl);
                }

                // 返回普通文字回應
                return candidateContent.Parts[0].Text ?? "無回應";
            }
            catch (Exception ex)
            {
                return $"無法處理請求: {ex.Message}";
            }
        }
        
        public async Task<string> GetResponseFromTextAsync(string prompt)
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
            
            // 準備請求
            var requestUrl = $"{API_BASE_URL}{modelName}:generateContent?key={apiKey}";
            
            // 添加用戶訊息到對話歷史
            _conversation.Add(new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = prompt } }
            });
            
            // 構建請求
            var request = new GeminiRequest
            {
                SystemInstruction = new SystemInstruction
                {
                    Parts = new List<Part> { new Part { Text = systemPrompt } }
                },
                Contents = _conversation,
                Tools = GetTools(hasImages: false)
            };
            
            try
            {
                Console.WriteLine($"發送純文字請求到 Gemini API... (模型: {modelName})");
                
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(requestUrl, content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Gemini API 錯誤: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    Console.WriteLine($"響應內容: {responseContent}");
                    return $"API 錯誤 {(int)response.StatusCode}: {responseContent}";
                }
                
                Console.WriteLine("成功獲取響應!");
                
                // 反序列化回應
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                
                // 檢查是否有回應
                if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
                {
                    return "無回應";
                }

                var candidate = geminiResponse.Candidates[0];
                var candidateContent = candidate.Content;

                // 將回應添加到對話歷史
                _conversation.Add(candidateContent);
                
                // 檢查是否為函數調用回應
                if (candidateContent.Parts.Count > 0 && candidateContent.Parts[0].FunctionCall != null)
                {
                    // 處理函數調用
                    return await HandleFunctionCallAsync(candidateContent.Parts[0].FunctionCall, apiKey, modelName, requestUrl);
                }
                
                // 返回普通文字回應
                return candidateContent.Parts[0].Text ?? "無回應";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling Gemini API: {ex.Message}");
                return $"無法處理請求: {ex.Message}";
            }
        }
        
        private List<Tool> GetTools(bool hasImages = false)
        {
            var tools = new List<Tool>();
            var toolMode = _settingsService.GetToolMode();
            
            // 根據是否有截圖決定使用哪種工具
            if (hasImages)
            {
                // 有截圖時使用 Google 搜尋
                tools.Add(Tool.CreateGoogleSearch());
            }
            else
            {
                // 沒有截圖時使用函數調用
                tools.Add(GetFunctionDeclarations());
            }
            
            return tools;
        }
        
        private Tool GetFunctionDeclarations()
        {
            // 建立可用的函數聲明列表
            var declarations = new List<FunctionDeclaration>
            {
                new FunctionDeclaration
                {
                    Name = "getWeatherInfo",
                    Description = "取得指定位置的當前天氣資訊",
                    Parameters = new Parameter
                    {
                        Type = "object",
                        Properties = new Dictionary<string, PropertyInfo>
                        {
                            {
                                "location", new PropertyInfo
                                {
                                    Type = "string",
                                    Description = "要查詢的城市和地區，例如：台北市、New York, NY"
                                }
                            }
                        },
                        Required = new List<string> { "location" }
                    }
                }
                // 可以在這裡添加更多函數聲明
            };
            
            return Tool.CreateFunctionDeclarations(declarations);
        }
        
        private async Task<string> HandleFunctionCallAsync(FunctionCall functionCall, string apiKey, string modelName, string requestUrl)
        {
            // 執行函數並取得結果
            var functionResult = await ExecuteFunctionAsync(functionCall);
            
            // 將函數結果添加到對話歷史
            _conversation.Add(new Content
            {
                Role = "function",
                Parts = new List<Part>
                {
                    new Part
                    {
                        FunctionResponse = new FunctionResponse
                        {
                            Name = functionCall.Name,
                            Response = functionResult
                        }
                    }
                }
            });
            
            var request = new GeminiRequest
            {
                Contents = _conversation,
                Tools = GetTools(hasImages: false)
            };
            
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"API 錯誤 {(int)response.StatusCode}: {errorContent}";
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            
            if (geminiResponse?.Candidates == null || geminiResponse.Candidates.Count == 0)
            {
                return "無回應";
            }
            
            var finalResponse = geminiResponse.Candidates[0].Content;
            
            // 將最終回覆添加到對話歷史
            _conversation.Add(finalResponse);
            
            // 檢查是否是另一個函數調用
            if (finalResponse.Parts.Count > 0 && finalResponse.Parts[0].FunctionCall != null)
            {
                // 遞歸處理另一個函數調用
                return await HandleFunctionCallAsync(finalResponse.Parts[0].FunctionCall, apiKey, modelName, requestUrl);
            }
            
            // 返回普通文字回應
            return finalResponse.Parts[0].Text ?? "無回應";
        }
        
        private async Task<object> ExecuteFunctionAsync(FunctionCall functionCall)
        {
            // 根據函數名稱執行對應的函數
            switch (functionCall.Name)
            {
                case "getWeatherInfo":
                    string location = functionCall.Args.GetProperty("location").GetString() ?? string.Empty;
                    Console.WriteLine($"獲取天氣信息: {location}");
                    // 這裡通常會調用實際的天氣 API，這裡只是模擬
                    return await GetWeatherInfoAsync(location);
                default:
                    throw new NotImplementedException($"函數 {functionCall.Name} 未實現");
            }
        }
        
        private async Task<WeatherInfo> GetWeatherInfoAsync(string location)
        {
            // 模擬獲取天氣信息的操作，實際應用中應該調用天氣 API
            await Task.Delay(500); // 模擬網絡延遲
            
            // 模擬不同地點的天氣
            var random = new Random();
            double temp = Math.Round(random.NextDouble() * 30 + 10, 1); // 10-40°C
            
            string[] conditions = { "晴朗", "多雲", "雨天", "陰天", "大風" };
            string condition = conditions[random.Next(conditions.Length)];
            
            return new WeatherInfo
            {
                Temperature = temp,
                Unit = "攝氏",
                Condition = condition
            };
        }
    }
}