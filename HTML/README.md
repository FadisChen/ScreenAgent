# Screen Agent 網頁版

## 簡介

Screen Agent 網頁版是桌面版 Screen Agent 的 HTML+JavaScript 改寫版本，使用現代 Web API 實現螢幕錄製和 Gemini AI 分析功能。透過網頁版，您可以在任何支援現代瀏覽器的設備上使用 Screen Agent 的強大功能，無需安裝任何軟體。

## 功能特色

- **螢幕截圖分析**：自動截取螢幕畫面並結合用戶問題發送到 Gemini AI 分析
- **即時問答**：針對螢幕上顯示的內容進行提問並取得即時回應
- **語音朗讀**：支援文字轉語音功能，可將 AI 回應以語音方式播放
- **對話歷史**：保存問答記錄，方便查閱
- **自訂設定**：可調整 API Key、模型選擇、截圖頻率等參數
- **懸浮式介面**：可拖動定位的控制面板和對話歷史面板

## 使用需求

- 現代瀏覽器（Chrome 72+ / Edge 79+ / Firefox 66+）
- HTTPS 環境（網頁錄製 API 僅在安全環境下可用）
- Gemini API 金鑰

## 快速上手

1. 開啟 `index.html` 檔案（必須透過 HTTPS 存取或在本地使用 localhost）
2. 點擊設定按鈕（⚙️），輸入您的 Gemini API 金鑰
3. 點擊錄製按鈕（🔴）開始截取螢幕畫面
4. 在文字框中輸入您的問題
5. 點擊提交按鈕（➡️）發送查詢
6. 查看右側對話視窗中的回應結果

## 本地開發與測試

由於瀏覽器安全限制，使用 `navigator.mediaDevices.getDisplayMedia` API 必須在 HTTPS 環境或 localhost 下運行。以下是幾種本地測試方法：

### 使用 Python 建立簡易伺服器

```bash
# Python 3
python -m http.server 8000

# Python 2
python -m SimpleHTTPServer 8000
```

然後在瀏覽器中訪問 `http://localhost:8000`

### 使用 Node.js 和 http-server

```bash
# 安裝 http-server
npm install -g http-server

# 啟動伺服器
http-server -p 8000
```

然後在瀏覽器中訪問 `http://localhost:8000`

## 隱私與安全

- 所有資料處理都在本地完成，只有截圖和查詢文字會發送到 Gemini API
- API 金鑰和設定僅存儲在您的本地瀏覽器中
- 不會收集或分享任何個人資訊
- 請注意分享螢幕時可能會顯示敏感資訊，建議避免錄製含有隱私資訊的畫面

## 技術細節

- **螢幕錄製**：使用 `navigator.mediaDevices.getDisplayMedia` Web API
- **圖片處理**：使用 Canvas API 擷取視訊幀並轉換為圖片
- **AI 整合**：直接使用 Gemini REST API
- **語音合成**：使用 Web Speech API (SpeechSynthesis)
- **本地儲存**：使用 localStorage 儲存使用者設定

## 已知限制

- 只能在 HTTPS 環境或 localhost 下運行
- 某些瀏覽器可能限制語音功能的使用
- Gemini API 有使用配額限制，請參閱 Google API 文件

## 問題排解

### 無法存取螢幕錄製功能

- 確認您正在使用支援的瀏覽器
- 確認頁面是透過 HTTPS 或 localhost 存取
- 確認已授予頁面螢幕錄製權限

### 無法獲取 Gemini API 回應

- 確認 API 金鑰已正確設定
- 確認網路連接正常
- 檢查瀏覽器控制台是否有錯誤訊息

### 語音功能無法正常工作

- 確認瀏覽器支援 Web Speech API
- 檢查是否已啟用語音功能
- 嘗試選擇不同的語音選項