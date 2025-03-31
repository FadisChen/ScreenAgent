namespace ScreenAgent.Models
{
    public class AppSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public bool ShowConversationHistory { get; set; } = true;
        public int CaptureFrequencyInSeconds { get; set; } = 1;
        public string TextToSpeechVoiceName { get; set; } = string.Empty;
        public string GeminiModel { get; set; } = "gemini-2.0-flash-exp";
        public bool EnableTextToSpeech { get; set; } = true;
        
        // 語音識別相關設定 (免費方案)
        public string SpeechLanguage { get; set; } = "zh-TW";
        public bool EnableSpeechToText { get; set; } = true;
        
        public string SystemPrompt { get; set; } = @"你是一個智慧回覆助手，能夠分析桌面截圖並回答使用者的提問。你的回應方式遵循以下規則：
與圖片相關的問題：
仔細分析圖片內容，根據使用者的提問提供準確、清楚的指導與回答。
若問題涉及技術支援，提供具體步驟或建議。
若問題模糊，請要求使用者提供更多資訊，以確保回答準確。
沒有圖片或與圖片無關的問題：
以簡短、直接的方式進行回應，保持自然的對話風格。
若問題超出你的知識範圍，可使用工具查詢最新資訊後回答。
回覆風格：
內容簡潔明瞭，避免冗長或不必要的細節。
禁止使用表情符號、特殊符號或非文字內容。
你應該始終確保回應的專業性與實用性，並根據問題的性質適當調整回覆方式，一律以繁體中文回覆。";
    }
}