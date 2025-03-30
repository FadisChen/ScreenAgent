/**
 * Screen Agent 網頁版 - 主應用程式邏輯
 * 整合各服務模組，處理UI互動和業務邏輯
 */
document.addEventListener('DOMContentLoaded', () => {
    // UI元素
    const controlPanel = document.getElementById('control-panel');
    const conversationPanel = document.getElementById('conversation-panel');
    const btnRecord = document.getElementById('btn-record');
    const btnStop = document.getElementById('btn-stop');
    const btnSubmit = document.getElementById('btn-submit');
    const btnSettings = document.getElementById('btn-settings');
    const btnInfo = document.getElementById('btn-info');
    const btnClearHistory = document.getElementById('btn-clear-history');
    const btnToggleHistory = document.getElementById('btn-toggle-history');
    const promptInput = document.getElementById('prompt-input');
    const statusText = document.getElementById('status-text');
    const conversationHistory = document.getElementById('conversation-history');
    
    // 設定相關元素
    const apiKeyInput = document.getElementById('api-key');
    const geminiModelInput = document.getElementById('gemini-model');
    const captureFrequencyInput = document.getElementById('capture-frequency');
    const frequencyValueDisplay = document.getElementById('frequency-value');
    const showHistoryCheckbox = document.getElementById('show-history');
    const enableTtsCheckbox = document.getElementById('enable-tts');
    const voiceSelect = document.getElementById('voice-select');
    const systemPromptTextarea = document.getElementById('system-prompt');
    const saveSettingsBtn = document.getElementById('save-settings');
    
    // 模態對話框
    const settingsModal = new bootstrap.Modal(document.getElementById('settings-modal'));
    const infoModal = new bootstrap.Modal(document.getElementById('info-modal'));
    
    // 狀態變數
    let isCapturing = false;
    
    /**
     * 初始化應用程式
     */
    function initApp() {
        // 載入使用者設定
        loadSettings();
        
        // 綁定 UI 事件
        bindEvents();
        
        // 更新UI狀態
        updateHistoryVisibility();
        
        // 初始化語音選項
        updateVoiceOptions();
        
        // 註冊螢幕截圖批次監聽器
        screenCaptureService.addScreensCaptureBatchListener(handleScreensCaptureBatch);
        
        // 設置面板可拖動
        makePanelsDraggable();
        
        // 設置初始狀態
        setStatus('準備就緒');
    }
    
    /**
     * 綁定UI事件
     */
    function bindEvents() {
        // 錄製按鈕
        btnRecord.addEventListener('click', startCapturing);
        btnStop.addEventListener('click', stopCapturing);
        
        // 提交按鈕
        btnSubmit.addEventListener('click', submitQuery);
        
        // 按Enter提交
        promptInput.addEventListener('keydown', e => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                submitQuery();
            }
        });
        
        // 設定按鈕
        btnSettings.addEventListener('click', () => settingsModal.show());
        
        // 關於按鈕
        btnInfo.addEventListener('click', () => infoModal.show());
        
        // 清除歷史按鈕
        btnClearHistory.addEventListener('click', clearHistory);
        
        // 切換歷史按鈕
        btnToggleHistory.addEventListener('click', toggleHistoryPanel);
        
        // 儲存設定按鈕
        saveSettingsBtn.addEventListener('click', saveSettings);
        
        // 截圖頻率滑桿
        captureFrequencyInput.addEventListener('input', () => {
            frequencyValueDisplay.textContent = captureFrequencyInput.value;
        });
        
        // 啟用/禁用語音選擇
        enableTtsCheckbox.addEventListener('change', () => {
            voiceSelect.disabled = !enableTtsCheckbox.checked;
        });
    }
    
    /**
     * 設置面板可拖動
     */
    function makePanelsDraggable() {
        // 控制面板拖動
        controlPanel.querySelector('.panel-header').addEventListener('mousedown', function(e) {
            const panel = controlPanel;
            const initialX = e.clientX - panel.offsetLeft;
            const initialY = e.clientY - panel.offsetTop;
            
            function movePanel(e) {
                panel.style.left = `${e.clientX - initialX}px`;
                panel.style.top = `${e.clientY - initialY}px`;
                panel.style.transform = 'none';
            }
            
            function stopMoving() {
                document.removeEventListener('mousemove', movePanel);
                document.removeEventListener('mouseup', stopMoving);
            }
            
            document.addEventListener('mousemove', movePanel);
            document.addEventListener('mouseup', stopMoving);
        });
        
        // 對話歷史面板拖動
        conversationPanel.querySelector('.panel-header').addEventListener('mousedown', function(e) {
            const panel = conversationPanel;
            const initialX = e.clientX - panel.offsetLeft;
            const initialY = e.clientY - panel.offsetTop;
            
            function movePanel(e) {
                panel.style.left = `${e.clientX - initialX}px`;
                panel.style.top = `${e.clientY - initialY}px`;
            }
            
            function stopMoving() {
                document.removeEventListener('mousemove', movePanel);
                document.removeEventListener('mouseup', stopMoving);
            }
            
            document.addEventListener('mousemove', movePanel);
            document.addEventListener('mouseup', stopMoving);
        });
    }
    
    /**
     * 載入應用程式設定
     */
    function loadSettings() {
        // 載入API金鑰設定
        apiKeyInput.value = settingsService.getApiKey();
        
        // 載入Gemini模型設定
        geminiModelInput.value = settingsService.getGeminiModel();
        
        // 載入截圖頻率設定
        const frequency = settingsService.getCaptureFrequencyInMilliseconds() / 1000;
        captureFrequencyInput.value = frequency;
        frequencyValueDisplay.textContent = frequency;
        
        // 載入顯示對話歷史設定
        showHistoryCheckbox.checked = settingsService.getShowConversationHistory();
        
        // 載入啟用語音設定
        enableTtsCheckbox.checked = settingsService.getEnableTextToSpeech();
        voiceSelect.disabled = !enableTtsCheckbox.checked;
        
        // 載入系統提示詞
        systemPromptTextarea.value = settingsService.getSystemPrompt();
    }
    
    /**
     * 更新語音選項
     */
    function updateVoiceOptions() {
        // 清空現有選項
        voiceSelect.innerHTML = '';
        
        // 獲取可用語音
        const voices = speechService.getAvailableVoices();
        
        // 添加語音選項
        voices.forEach(voice => {
            const option = document.createElement('option');
            option.value = voice.name;
            option.textContent = `${voice.name} (${voice.lang})`;
            voiceSelect.appendChild(option);
        });
        
        // 設定當前選中的語音
        const currentVoice = settingsService.getTextToSpeechVoiceName();
        if (currentVoice) {
            const option = Array.from(voiceSelect.options).find(opt => opt.value === currentVoice);
            if (option) {
                option.selected = true;
            }
        }
    }
    
    /**
     * 儲存使用者設定
     */
    function saveSettings() {
        try {
            // 開始批次更新設定
            settingsService.beginUpdate();
            
            // 儲存各項設定
            settingsService.setApiKey(apiKeyInput.value);
            settingsService.setGeminiModel(geminiModelInput.value);
            settingsService.setShowConversationHistory(showHistoryCheckbox.checked);
            settingsService.setCaptureFrequencyInSeconds(parseFloat(captureFrequencyInput.value));
            settingsService.setEnableTextToSpeech(enableTtsCheckbox.checked);
            settingsService.setSystemPrompt(systemPromptTextarea.value);
            
            // 儲存語音設定
            if (voiceSelect.value) {
                settingsService.setTextToSpeechVoiceName(voiceSelect.value);
            }
            
            // 完成批次更新，一次性保存所有設定
            settingsService.endUpdate();
            
            // 更新UI
            updateHistoryVisibility();
            
            // 關閉設定對話框
            settingsModal.hide();
            
            // 顯示成功提示
            setStatus('設定已儲存');
        } catch (error) {
            console.error('儲存設定時發生錯誤:', error);
            alert(`儲存設定時發生錯誤: ${error.message}`);
        }
    }
    
    /**
     * 開始螢幕錄製
     */
    async function startCapturing() {
        // 檢查API金鑰是否已設定
        if (!settingsService.getApiKey()) {
            alert('請先在設定中設定您的 Gemini API Key');
            settingsModal.show();
            return;
        }
        
        try {
            // 停止正在播放的語音
            speechService.stop();
            
            // 開始螢幕錄製
            await screenCaptureService.startCapturing();
            
            // 更新UI
            isCapturing = true;
            btnRecord.classList.add('d-none');
            btnStop.classList.remove('d-none');
            
            // 更新狀態
            setStatus('截圖中... 請輸入問題並按「提交」發送查詢');
        } catch (error) {
            console.error('啟動螢幕錄製失敗:', error);
            alert(`啟動螢幕錄製失敗: ${error.message}`);
        }
    }
    
    /**
     * 停止螢幕錄製
     */
    function stopCapturing() {
        try {
            // 提交當前收集的截圖
            submitCapturedScreens();
            
            // 停止螢幕錄製
            screenCaptureService.stopCapturing();
            
            // 更新UI
            isCapturing = false;
            btnRecord.classList.remove('d-none');
            btnStop.classList.add('d-none');
            
            // 更新狀態
            setStatus('處理中...');
        } catch (error) {
            console.error('停止螢幕錄製失敗:', error);
            alert(`停止螢幕錄製失敗: ${error.message}`);
        }
    }
    
    /**
     * 提交當前收集的截圖
     */
    function submitCapturedScreens() {
        // 獲取使用者問題
        const userPrompt = promptInput.value.trim();
        
        // 檢查問題是否為空
        if (!userPrompt) {
            alert('請輸入問題再提交');
            return;
        }
        
        // 停止正在播放的語音
        speechService.stop();
        
        // 更新狀態
        setStatus('正在處理您的請求...');
        
        if (isCapturing) {
            // 如果正在錄製，提交當前截圖批次
            screenCaptureService.submitCapturedBatch();
        } else {
            // 如果沒有在錄製，則直接提交文本查詢
            submitTextOnlyQuery(userPrompt);
        }
        
        // 清空輸入框
        promptInput.value = '';
    }
    
    /**
     * 提交僅文字查詢
     * @param {string} userPrompt - 使用者問題
     */
    async function submitTextOnlyQuery(userPrompt) {
        try {
            // 更新狀態
            setStatus('處理中...');
            
            // 調用 Gemini API 處理查詢
            await processGeminiRequest([], userPrompt);
        } catch (error) {
            console.error('處理失敗:', error);
            alert(`處理失敗: ${error.message}`);
            setStatus('發生錯誤，請重試');
        }
    }
    
    /**
     * 處理螢幕截圖批次事件
     * @param {Array<string>} imageDataList - 截圖資料陣列
     */
    async function handleScreensCaptureBatch(imageDataList) {
        try {
            // 獲取使用者問題
            const userPrompt = promptInput.value.trim();
            
            // 更新狀態
            setStatus(`處理中... (${imageDataList.length} 張截圖)`);
            
            // 處理包含截圖的請求
            await processGeminiRequest(imageDataList, userPrompt);
        } catch (error) {
            console.error('處理失敗:', error);
            alert(`處理失敗: ${error.message}`);
            setStatus('發生錯誤，請重試');
        }
    }
    
    /**
     * 處理 Gemini 請求
     * @param {Array<string>} imageDataList - 截圖資料陣列
     * @param {string} userPrompt - 使用者問題
     */
    async function processGeminiRequest(imageDataList, userPrompt) {
        // 更新狀態
        setStatus('處理中...');
        
        // 調用 Gemini API
        const response = await geminiService.getResponseFromImageBatchAndTextAsync(imageDataList, userPrompt);
        
        // 清除特殊標記
        const cleanResponse = response.replace(/\*\*/g, '');
        
        // 顯示回應
        displayResponse(userPrompt, cleanResponse);
        
        // 文字轉語音
        speechService.speak(cleanResponse);
        
        // 更新狀態
        setStatus('已完成回應');
    }
    
    /**
     * 顯示回應
     * @param {string} userPrompt - 使用者問題
     * @param {string} aiResponse - AI 回應
     */
    function displayResponse(userPrompt, aiResponse) {
        // 創建時間戳記
        const timestamp = new Date().toLocaleString('zh-TW');
        
        // 創建對話元素
        const conversationItem = document.createElement('div');
        conversationItem.className = 'conversation-item';
        conversationItem.innerHTML = `
            <div class="user-prompt">${escapeHTML(userPrompt)}</div>
            <div class="ai-response">${escapeHTML(aiResponse)}</div>
            <div class="timestamp">${timestamp}</div>
        `;
        
        // 添加到對話歷史
        conversationHistory.prepend(conversationItem);
    }
    
    /**
     * 清空對話歷史
     */
    function clearHistory() {
        if (confirm('確定要清除所有對話歷史嗎？')) {
            conversationHistory.innerHTML = '';
        }
    }
    
    /**
     * 切換對話歷史面板顯示
     */
    function toggleHistoryPanel() {
        if (conversationPanel.style.display === 'none') {
            conversationPanel.style.display = 'flex';
            settingsService.setShowConversationHistory(true);
        } else {
            conversationPanel.style.display = 'none';
            settingsService.setShowConversationHistory(false);
        }
    }
    
    /**
     * 更新對話歷史面板顯示狀態
     */
    function updateHistoryVisibility() {
        const showHistory = settingsService.getShowConversationHistory();
        conversationPanel.style.display = showHistory ? 'flex' : 'none';
    }
    
    /**
     * 更新狀態文字
     * @param {string} message - 狀態消息
     */
    function setStatus(message) {
        statusText.textContent = message;
    }
    
    /**
     * HTML轉義，防止XSS攻擊
     * @param {string} text - 原始文字
     * @returns {string} 轉義後的HTML
     */
    function escapeHTML(text) {
        return text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;')
            .replace(/\n/g, '<br>');
    }
    
    // 提交查詢
    function submitQuery() {
        const userPrompt = promptInput.value.trim();
        if (!userPrompt) {
            alert('請輸入問題再提交');
            return;
        }
        
        submitCapturedScreens();
    }
    
    // 初始化應用程式
    initApp();
}); 