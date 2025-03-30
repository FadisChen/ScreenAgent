# Screen Agent 網頁版實現說明

本文檔記錄 Screen Agent 網頁版的實現細節和技術注意事項，供開發者參考。

## 開發環境

- 純 HTML + CSS + JavaScript
- Bootstrap 5 用於 UI 元素和樣式
- Bootstrap Icons 用於圖標
- 不依賴任何後端服務
- 僅使用瀏覽器原生 API

## 核心技術實現

### 1. 螢幕錄製功能

螢幕錄製功能使用 `navigator.mediaDevices.getDisplayMedia` Web API 實現。該 API 可以請求用戶分享螢幕，並返回媒體流。

```javascript
// 請求用戶分享螢幕
const stream = await navigator.mediaDevices.getDisplayMedia({
    video: { cursor: "always" },
    audio: false
});
```

獲取到媒體流後，我們創建一個隱藏的 video 元素來接收該流，然後使用 Canvas API 定期從視頻流截取畫面：

```javascript
// 創建隱藏的 video 元素接收媒體流
videoElement = document.createElement('video');
videoElement.srcObject = stream;
videoElement.autoplay = true;
videoElement.style.display = 'none';
document.body.appendChild(videoElement);

// 使用 Canvas 截取視頻幀
const canvas = document.createElement('canvas');
canvas.width = videoElement.videoWidth;
canvas.height = videoElement.videoHeight;
const ctx = canvas.getContext('2d');
ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);

// 轉換為 base64 圖片數據
const imageData = canvas.toDataURL('image/jpeg', 0.85);
```

### 2. Gemini API 集成

Gemini API 集成通過直接使用 REST API 實現。我們將截取的圖片轉換為 base64 格式，然後與用戶問題一起發送到 Gemini API：

```javascript
// 構建請求
const parts = [{ text: prompt }];
for (const imageData of imageDataList) {
    parts.push({
        inlineData: {
            mimeType: "image/jpeg",
            data: imageData
        }
    });
}

const requestBody = {
    system_instruction: {
        parts: [{ text: systemPrompt }]
    },
    contents: [{ parts: parts }],
    tools: [{ google_search: {} }]
};

// 發送請求
const response = await fetch(
    `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key=${apiKey}`,
    {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(requestBody)
    }
);
```

### 3. 文字轉語音

文字轉語音功能使用 Web Speech API 的 SpeechSynthesis 介面實現：

```javascript
// 創建語音配置
const utterance = new SpeechSynthesisUtterance(text);

// 設置語音
const voice = speechSynthesis.getVoices().find(v => v.name === voiceName);
if (voice) {
    utterance.voice = voice;
    utterance.lang = voice.lang;
}

// 播放語音
speechSynthesis.speak(utterance);
```

### 4. 本地儲存

用戶設定使用 localStorage API 在本地儲存：

```javascript
// 保存設定
localStorage.setItem('screenAgent_settings', JSON.stringify(settings));

// 讀取設定
const savedSettings = localStorage.getItem('screenAgent_settings');
const settings = savedSettings ? JSON.parse(savedSettings) : defaultSettings;
```

## 模組設計

應用程式使用模組化設計，分為多個服務類：

1. **SettingsService**：管理用戶設定的讀取和保存
2. **ScreenCaptureService**：實現螢幕錄製和截圖功能
3. **GeminiService**：處理與 Gemini API 的通信
4. **SpeechService**：實現文字轉語音功能
5. **App（主模組）**：整合各服務模組，處理 UI 互動和業務邏輯

## 安全考量

### API 金鑰保護

API 金鑰僅保存在用戶的本地存儲中，不會發送到除 Google API 以外的任何服務。但由於這是一個純前端應用，API 金鑰仍然暴露在客戶端，存在被濫用的風險。

在生產環境中，更安全的方法是：

1. 使用代理伺服器中轉 API 請求
2. 在代理伺服器中添加 API 金鑰
3. 實施速率限制和 IP 限制

### 跨站腳本攻擊（XSS）防護

應用程式對顯示的用戶輸入和 AI 回應進行 HTML 轉義，防止 XSS 攻擊：

```javascript
function escapeHTML(text) {
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;')
        .replace(/\n/g, '<br>');
}
```

## 已知限制和可能的改進

### 已知限制

1. 螢幕錄製 API 需要 HTTPS 或 localhost 環境
2. 文字轉語音 API 在某些瀏覽器中有限制（如 Safari）
3. 本地存儲容量有限（通常為 5MB）
4. Gemini API 有使用配額和費率限制

### 可能的改進

1. **增加錄製選項**：支援錄製特定應用窗口或瀏覽器標籤
2. **圖片壓縮**：進一步優化圖片大小，減少 API 請求大小
3. **離線支援**：增加 Service Worker 支援，實現離線功能
4. **多語言支援**：增加界面語言切換功能
5. **螢幕錄製**：支援視頻錄製和保存功能（不僅是截圖）
6. **對話管理**：支援多個對話會話和對話匯出功能
7. **添加 PWA 支援**：使應用可以安裝到桌面

## 疑難排解

### CORS 問題

由於瀏覽器的同源策略，如果直接在本地文件系統（file:///）中打開 HTML 文件，可能會遇到 CORS 錯誤。解決方法是使用本地伺服器（如 Python 的 http.server 或 Node.js 的 http-server）提供文件。

### API 金鑰問題

確保 API 金鑰有正確的權限，並且啟用了 Gemini API。API 金鑰錯誤通常會在瀏覽器控制台顯示具體錯誤信息。

### 媒體設備訪問權限

某些環境（如企業網絡或受限設備）可能禁止訪問媒體設備或限制 getDisplayMedia API。在這種情況下，應用程式將無法正常工作。

## 瀏覽器兼容性

| 瀏覽器 | getDisplayMedia | SpeechSynthesis | 最低版本 |
|-------|-----------------|-----------------|--------|
| Chrome | ✅ | ✅ | 72+ |
| Edge | ✅ | ✅ | 79+ |
| Firefox | ✅ | ✅ | 66+ |
| Safari | ✅ (14.0+) | ✅ (7.0+) | 14.0+ |
| Opera | ✅ | ✅ | 60+ |

## 總結

Screen Agent 網頁版展示了如何利用現代 Web API 實現原本需要桌面應用的功能。透過純前端技術，我們實現了螢幕錄製、圖像處理、AI 分析和語音合成等功能，為用戶提供了無需安裝即可使用的便捷體驗。

---

最後更新: 2025 年 3 月 