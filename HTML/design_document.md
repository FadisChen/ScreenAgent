# Screen Agent 網頁版設計文件

## 1. 專案概述

將現有的桌面版Screen Agent應用程式改寫為HTML+JavaScript的網頁版本，實現螢幕錄製與AI分析功能，無需後端程式。

### 1.1 核心功能

- 使用`navigator.mediaDevices.getDisplayMedia` API實現螢幕錄製和截圖
- 使用Gemini API進行圖片分析和問題回答
- 文字轉語音功能，用於朗讀AI回應
- 對話歷史紀錄顯示與管理
- 使用者設定管理（API Key、截圖頻率、語音設定等）

### 1.2 技術選擇

- **前端框架**：純HTML+CSS+JavaScript (不使用React/Vue等框架)
- **UI框架**：使用Bootstrap 5提供響應式設計
- **本地儲存**：使用localStorage儲存使用者設定
- **螢幕錄製**：使用navigator.mediaDevices.getDisplayMedia API
- **AI整合**：直接使用Gemini REST API
- **語音合成**：使用Web Speech API (SpeechSynthesis)

## 2. 系統架構

### 2.1 檔案結構

```
HTML/
  ├── index.html          # 主頁面
  ├── css/
  │   ├── styles.css      # 主要樣式表
  │   └── bootstrap.min.css  # Bootstrap樣式
  ├── js/
  │   ├── app.js          # 主應用程式邏輯
  │   ├── screenCapture.js # 螢幕錄製和截圖功能
  │   ├── geminiService.js # Gemini API調用
  │   ├── speechService.js # 文字轉語音功能
  │   └── settingsService.js # 設定管理
  └── assets/             # 圖示和其他資源
      └── icons/
```

### 2.2 核心模組

#### 2.2.1 螢幕錄製模組 (screenCapture.js)

負責實現螢幕錄製和截圖功能：
- 使用`navigator.mediaDevices.getDisplayMedia`存取使用者螢幕
- 定時擷取螢幕截圖，存儲為圖片資料
- 提供開始/停止錄製功能
- 管理截圖批次，並提供提交功能

#### 2.2.2 Gemini服務模組 (geminiService.js)

負責與Gemini API溝通：
- 處理API金鑰驗證
- 發送截圖和文字查詢到Gemini API
- 處理API回應，並傳回格式化結果
- 支援純文字查詢功能

#### 2.2.3 語音服務模組 (speechService.js)

負責文字轉語音功能：
- 使用Web Speech API進行語音合成
- 支援語音控制（播放/暫停/停止）
- 支援選擇不同語音選項

#### 2.2.4 設定服務模組 (settingsService.js)

負責管理使用者設定：
- 使用localStorage儲存設定資料
- 提供讀取/更新設定的方法
- 支援批次更新設定

### 2.3 使用者介面

#### 2.3.1 主介面

- 懸浮式面板，可拖動定位
- 錄製控制按鈕（開始/停止）
- 文字輸入區域和提交按鈕
- 狀態顯示區域

#### 2.3.2 對話歷史介面

- 可顯示/隱藏的側邊面板
- 顯示使用者問題和AI回應
- 支援複製回應文字
- 時間戳記顯示

#### 2.3.3 設定介面

- API Key設定
- Gemini模型選擇
- 截圖頻率調整
- 語音設定（開啟/關閉、語音選擇）
- 系統提示詞設定

## 3. 實作計劃

### 3.1 開發步驟

1. **建立基本HTML/CSS架構**
   - 設計主介面和對話歷史面板
   - 實現響應式佈局

2. **實現核心服務模組**
   - 開發設定服務，實現本地儲存功能
   - 開發螢幕錄製模組，實現截圖功能
   - 開發Gemini服務，實現API調用功能
   - 開發語音服務，實現文字轉語音功能

3. **整合UI和服務模組**
   - 連接UI元素與服務模組功能
   - 實現介面狀態管理
   - 處理使用者交互邏輯

4. **改進與優化**
   - 完善錯誤處理和使用者體驗
   - 優化性能，減少資源占用
   - 增強安全性，保護API金鑰

### 3.2 安全考量

- API金鑰只在本地儲存，不發送到任何第三方服務
- 使用HTTPS協議保護數據傳輸
- 提示使用者注意螢幕錄製的隱私風險
- 提供清除設定選項

## 4. 技術細節與API用法

### 4.1 螢幕錄製API示例

```javascript
// 請求螢幕錄製權限並獲取媒體流
async function startCapturing() {
  try {
    const stream = await navigator.mediaDevices.getDisplayMedia({
      video: { mediaSource: "screen" }
    });
    
    // 處理媒體流，例如擷取截圖
    const videoTrack = stream.getVideoTracks()[0];
    // ...
  } catch (error) {
    console.error("螢幕錄製失敗:", error);
  }
}
```

### 4.2 Gemini API調用示例

```javascript
async function callGeminiAPI(imageDataList, prompt, apiKey) {
  const parts = [{ text: prompt }];
  
  // 添加所有圖片
  for (const imageData of imageDataList) {
    parts.push({
      inlineData: {
        mimeType: "image/jpeg",
        data: imageData // Base64編碼的圖片
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
  
  return await response.json();
}
```

### 4.3 文字轉語音示例

```javascript
function speakText(text, voiceName) {
  const utterance = new SpeechSynthesisUtterance(text);
  
  // 設定語音
  const voices = window.speechSynthesis.getVoices();
  const voice = voices.find(v => v.name === voiceName) || 
                voices.find(v => v.lang === 'zh-TW') || 
                voices[0];
  
  utterance.voice = voice;
  utterance.lang = 'zh-TW';
  
  // 播放語音
  window.speechSynthesis.speak(utterance);
}

function stopSpeaking() {
  window.speechSynthesis.cancel();
}
```

## 5. 兼容性與限制

- **瀏覽器兼容性**：需要現代瀏覽器支援（Chrome 72+、Edge 79+、Firefox 66+）
- **安全限制**：只能在HTTPS環境下使用getDisplayMedia API
- **API限制**：遵循Gemini API的使用限制和配額
- **本地儲存**：localStorage存在大小限制（通常為5MB）

## 6. 總結

網頁版Screen Agent透過現代Web API實現與桌面版相同的功能，提供更佳的跨平台能力和可訪問性。透過純前端實現，無需安裝即可使用，同時保持核心功能的完整性和用戶體驗。

---

設計者：[設計者姓名]
日期：[日期]
版本：1.0 