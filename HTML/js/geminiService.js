/**
 * Gemini服務模組 - 負責與Gemini API溝通
 */
class GeminiService {
    /**
     * 建立Gemini服務
     * @param {SettingsService} settingsService - 設定服務實例
     */
    constructor(settingsService) {
        this.settingsService = settingsService;
        this.API_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        this.DEFAULT_MODEL = "gemini-2.0-flash-exp";
    }

    /**
     * 從截圖批次和文字獲取回應
     * @param {Array<string>} imageDataList - Base64格式的截圖資料陣列
     * @param {string} prompt - 提問文字
     * @returns {Promise<string>} AI回應文字
     */
    async getResponseFromImageBatchAndTextAsync(imageDataList, prompt) {
        // 取得 API Key
        const apiKey = this.settingsService.getApiKey();
        if (!apiKey) {
            throw new Error("API Key 未設定");
        }

        // 從設定中取得模型名稱
        const modelName = this.settingsService.getGeminiModel() || this.DEFAULT_MODEL;

        // 從設定中取得系統提示詞
        const systemPrompt = this.settingsService.getSystemPrompt();

        // 準備API請求URL
        const requestUrl = `${this.API_BASE_URL}${modelName}:generateContent?key=${apiKey}`;

        // 構建請求內容陣列，包含提示文字部分
        const parts = [{ text: prompt }];

        // 添加所有圖片 (如果有的話)
        if (imageDataList && imageDataList.length > 0) {
            for (const imageData of imageDataList) {
                parts.push({
                    inlineData: {
                        mimeType: "image/jpeg",
                        data: imageData
                    }
                });
            }
        }

        // 構建最終請求體 (符合最新 Gemini API 文檔格式)
        const requestBody = {
            system_instruction: {
                parts: [{ text: systemPrompt }]
            },
            contents: [{ parts: parts }],
            tools: [{ google_search: {} }]
        };

        try {
            console.log(`發送請求到 Gemini API... (模型: ${modelName}, 包含 ${imageDataList?.length || 0} 張圖片)`);

            // 發送API請求
            const response = await fetch(requestUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(requestBody)
            });

            // 解析回應
            const responseData = await response.json();

            // 檢查API回應是否成功
            if (!response.ok) {
                console.error(`Gemini API 錯誤: ${response.status} - ${response.statusText}`);
                console.error(`回應內容:`, responseData);
                return `API 錯誤 ${response.status}: ${responseData.error?.message || JSON.stringify(responseData)}`;
            }

            console.log("成功獲取響應!");

            try {
                // 從回應中提取文字內容
                return responseData.candidates[0].content.parts[0].text || "無回應";
            } catch (error) {
                console.error(`解析響應時發生錯誤:`, error);
                console.error(`原始響應:`, responseData);
                return "解析 API 響應時發生錯誤";
            }
        } catch (error) {
            console.error(`調用 Gemini API 時發生錯誤:`, error);
            return `無法處理請求: ${error.message}`;
        }
    }

    /**
     * 從單張圖片和文字獲取回應 (轉調用批次方法)
     * @param {string} imageData - Base64格式的圖片資料
     * @param {string} prompt - 提問文字
     * @returns {Promise<string>} AI回應文字
     */
    async getResponseFromImageAndTextAsync(imageData, prompt) {
        return await this.getResponseFromImageBatchAndTextAsync([imageData], prompt);
    }

    /**
     * 僅從文字獲取回應
     * @param {string} prompt - 提問文字
     * @returns {Promise<string>} AI回應文字
     */
    async getResponseFromTextAsync(prompt) {
        return await this.getResponseFromImageBatchAndTextAsync([], prompt);
    }
}

// 建立Gemini服務的實例
const geminiService = new GeminiService(settingsService); 