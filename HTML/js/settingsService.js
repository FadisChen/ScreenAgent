/**
 * 設定服務模組 - 用於管理應用程式設定的讀取和儲存
 */
class SettingsService {
    constructor() {
        this.configKey = 'screenAgent_settings';
        this.defaultSettings = {
            apiKey: '',
            showConversationHistory: true,
            captureFrequencyInSeconds: 1,
            textToSpeechVoiceName: '',
            geminiModel: 'gemini-2.0-flash-exp',
            enableTextToSpeech: true,
            systemPrompt: `你是一個智慧回覆助手，能夠分析桌面截圖並回答使用者的提問。你的回應方式遵循以下規則：
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
你應該始終確保回應的專業性與實用性，並根據問題的性質適當調整回覆方式，一律以繁體中文回覆。`
        };
        
        this.settings = this.loadSettings();
        this.autoSave = true;
    }

    /**
     * 從 localStorage 讀取設定
     * @returns {Object} 設定物件
     */
    loadSettings() {
        try {
            // 嘗試從 localStorage 讀取設定
            const savedSettings = localStorage.getItem(this.configKey);
            
            if (savedSettings) {
                // 解析儲存的 JSON 字串為物件
                return JSON.parse(savedSettings);
            }
            
            // 如果沒有找到設定，返回預設設定
            return {...this.defaultSettings};
        } catch (error) {
            console.error('無法讀取設定：', error);
            // 發生錯誤時，返回預設設定
            return {...this.defaultSettings};
        }
    }

    /**
     * 儲存設定到 localStorage
     */
    saveSettings() {
        try {
            // 將設定物件轉換為 JSON 字串，並儲存到 localStorage
            localStorage.setItem(this.configKey, JSON.stringify(this.settings));
        } catch (error) {
            console.error('儲存設定時發生錯誤：', error);
        }
    }

    /**
     * 重置所有設定為預設值
     */
    resetSettings() {
        this.settings = {...this.defaultSettings};
        this.saveSettings();
    }

    // 取得所有設定
    getAllSettings() {
        return {...this.settings};
    }

    // 設定 API 金鑰
    getApiKey() {
        return this.settings.apiKey || '';
    }

    setApiKey(key) {
        this.settings.apiKey = key;
        if (this.autoSave) this.saveSettings();
    }

    // 設定是否顯示對話歷史
    getShowConversationHistory() {
        return this.settings.showConversationHistory !== false;
    }

    setShowConversationHistory(show) {
        this.settings.showConversationHistory = show;
        if (this.autoSave) this.saveSettings();
    }

    // 設定擷取頻率
    getCaptureFrequencyInMilliseconds() {
        return (this.settings.captureFrequencyInSeconds || 1) * 1000;
    }

    setCaptureFrequencyInSeconds(seconds) {
        this.settings.captureFrequencyInSeconds = seconds;
        if (this.autoSave) this.saveSettings();
    }

    // 設定語音名稱
    getTextToSpeechVoiceName() {
        return this.settings.textToSpeechVoiceName || '';
    }

    setTextToSpeechVoiceName(name) {
        this.settings.textToSpeechVoiceName = name;
        if (this.autoSave) this.saveSettings();
    }

    // 設定 Gemini 模型
    getGeminiModel() {
        return this.settings.geminiModel || 'gemini-2.0-flash-exp';
    }

    setGeminiModel(model) {
        this.settings.geminiModel = model;
        if (this.autoSave) this.saveSettings();
    }

    // 設定是否啟用文字轉語音
    getEnableTextToSpeech() {
        return this.settings.enableTextToSpeech !== false;
    }

    setEnableTextToSpeech(enable) {
        this.settings.enableTextToSpeech = enable;
        if (this.autoSave) this.saveSettings();
    }

    // 設定系統提示詞
    getSystemPrompt() {
        return this.settings.systemPrompt || this.defaultSettings.systemPrompt;
    }

    setSystemPrompt(prompt) {
        this.settings.systemPrompt = prompt;
        if (this.autoSave) this.saveSettings();
    }

    // 批次更新設定
    beginUpdate() {
        this.autoSave = false;
    }

    endUpdate() {
        this.autoSave = true;
        this.saveSettings();
    }
}

// 建立設定服務的單例實例
const settingsService = new SettingsService(); 