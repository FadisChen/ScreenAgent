namespace ScreenAgent.Models
{
    public enum ToolMode
    {
        None,
        GoogleSearch,
        FunctionCalling
    }

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
        
        // 發送訊息時截圖設定
        public bool CaptureOnSend { get; set; } = true;
        
        // 工具模式設定
        public ToolMode ToolMode { get; set; } = ToolMode.GoogleSearch;
        
        public string SystemPrompt { get; set; } = @"你是一個智慧回覆助手，能夠分析桌面截圖並回答使用者的提問。你的回應方式遵循以下規則：
【圖片相關問題】
- 仔細分析圖片內容，根據使用者的提問提供準確、清楚的指導與回答。
- 若問題涉及技術支援，提供具體步驟或建議。
- 若問題模糊，請要求使用者提供更多資訊，以確保回答準確。
【圖片與文字提示不相關】
- 若使用者的文字提示與圖片內容無關，請忽略圖片，僅依據文字提示進行回答，可透過工具google_search查詢相關資訊，並保持回覆簡短、直接且自然。
【純文字問題】
- 以簡短、直接的方式進行回應，保持自然的對話風格。
- 若問題超出你的知識範圍，可使用相關工具處理後回答。
【回覆風格】
- 內容簡潔明瞭，避免冗長或不必要的細節。
- 禁止使用表情符號、特殊符號或非文字內容。
- 始終確保回應的專業性與實用性，並根據問題的性質適當調整回覆方式，一律以繁體中文回覆。";
    }
}