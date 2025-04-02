using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenAgent.Models
{
    #region 請求模型
    
    public class GeminiRequest
    {
        [JsonPropertyName("system_instruction")]
        public SystemInstruction? SystemInstruction { get; set; }
        
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = new List<Content>();
        
        [JsonPropertyName("tools")]
        public List<Tool>? Tools { get; set; }
    }
    
    public class SystemInstruction
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();
    }
    
    public class Content
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new List<Part>();
    }
    
    public class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        
        [JsonPropertyName("inlineData")]
        public InlineData? InlineData { get; set; }
        
        [JsonPropertyName("functionCall")]
        public FunctionCall? FunctionCall { get; set; }
        
        [JsonPropertyName("functionResponse")]
        public FunctionResponse? FunctionResponse { get; set; }
    }
    
    public class InlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = "image/jpeg";
        
        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }
    
    public class Tool
    {
        [JsonPropertyName("googleSearch")]
        public object? GoogleSearch { get; set; }
        
        [JsonPropertyName("functionDeclarations")]
        public List<FunctionDeclaration>? FunctionDeclarations { get; set; }
        
        // 建立 Google 搜尋工具
        public static Tool CreateGoogleSearch()
        {
            return new Tool { GoogleSearch = new {} };
        }
        
        // 建立函數聲明工具
        public static Tool CreateFunctionDeclarations(List<FunctionDeclaration> declarations)
        {
            return new Tool { FunctionDeclarations = declarations };
        }
    }
    
    public class FunctionDeclaration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("parameters")]
        public Parameter Parameters { get; set; } = new Parameter();
    }
    
    public class Parameter
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("properties")]
        public Dictionary<string, PropertyInfo> Properties { get; set; } = new Dictionary<string, PropertyInfo>();
        
        [JsonPropertyName("required")]
        public List<string>? Required { get; set; }
    }
    
    public class PropertyInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("enum")]
        public List<string>? Enum { get; set; }
    }
    
    public class FunctionCall
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("args")]
        public JsonElement Args { get; set; }
    }
    
    public class FunctionResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("response")]
        public object? Response { get; set; }
    }
    
    #endregion
    
    #region 回應模型
    
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; } = new List<Candidate>();
        
        [JsonPropertyName("promptFeedback")]
        public PromptFeedback? PromptFeedback { get; set; }
    }
    
    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; } = new Content();
        
        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
        
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
    
    public class PromptFeedback
    {
        [JsonPropertyName("blockReason")]
        public string? BlockReason { get; set; }
    }
    
    #endregion
    
    #region 功能實現模型
    
    // 天氣資訊類別，用於示範函數調用
    public class WeatherInfo
    {
        public double Temperature { get; set; }
        public string Unit { get; set; } = "攝氏";
        public string Condition { get; set; } = string.Empty;
    }
    
    #endregion
} 