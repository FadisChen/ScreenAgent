/**
 * 螢幕錄製模組 - 負責螢幕擷取和截圖功能
 */
class ScreenCaptureService {
    /**
     * 建立螢幕錄製服務
     * @param {SettingsService} settingsService - 設定服務實例
     */
    constructor(settingsService) {
        this.settingsService = settingsService;
        this.captureTimer = null;
        this.capturedImages = [];
        this.stream = null;
        this.videoElement = null;
        this.isCapturing = false;
        
        // 自訂事件：批次截圖完成
        this.onScreensCaptureBatch = new CustomEvent('screensCaptureBatch');
        this.screenCaptureBatchListeners = [];
    }

    /**
     * 註冊批次截圖事件監聽器
     * @param {Function} listener - 事件監聽器函式
     */
    addScreensCaptureBatchListener(listener) {
        this.screenCaptureBatchListeners.push(listener);
    }

    /**
     * 移除批次截圖事件監聽器
     * @param {Function} listener - 事件監聽器函式
     */
    removeScreensCaptureBatchListener(listener) {
        const index = this.screenCaptureBatchListeners.indexOf(listener);
        if (index !== -1) {
            this.screenCaptureBatchListeners.splice(index, 1);
        }
    }

    /**
     * 觸發批次截圖事件，並傳遞截圖資料
     * @param {Array<string>} imageDataList - Base64 格式的截圖資料陣列
     */
    triggerScreensCaptureBatch(imageDataList) {
        this.screenCaptureBatchListeners.forEach(listener => {
            listener(imageDataList);
        });
    }

    /**
     * 單次截圖並返回圖片資料
     * @returns {Promise<string>} Base64 格式的圖片資料
     */
    async captureScreenSingle() {
        if (!this.videoElement || !this.stream) {
            throw new Error('未開始螢幕錄製，無法截取螢幕');
        }

        try {
            // 創建 canvas 元素並設置尺寸
            const canvas = document.createElement('canvas');
            canvas.width = this.videoElement.videoWidth;
            canvas.height = this.videoElement.videoHeight;
            
            // 獲取 canvas 上下文並繪製視訊幀
            const ctx = canvas.getContext('2d');
            ctx.drawImage(this.videoElement, 0, 0, canvas.width, canvas.height);
            
            // 將 canvas 內容轉換為 base64 圖片資料 (JPEG格式, 85% 品質)
            const imageData = canvas.toDataURL('image/jpeg', 0.85);
            
            // 移除 base64 前綴，只保留實際資料部分
            return imageData.replace('data:image/jpeg;base64,', '');
        } catch (error) {
            console.error('Error capturing screen:', error);
            throw new Error(`無法執行螢幕截圖: ${error.message}`);
        }
    }

    /**
     * 捕獲螢幕並添加到集合
     */
    async captureScreen() {
        try {
            // 使用共用方法獲取截圖
            const screenImage = await this.captureScreenSingle();
            
            // 將截圖添加到集合中
            this.capturedImages.push(screenImage);
        } catch (error) {
            console.error('Error capturing screen:', error);
            // 不要在這裡拋出異常，因為這是在定時器的回調中，可能會導致應用崩潰
        }
    }

    /**
     * 開始螢幕錄製
     * @returns {Promise<void>}
     */
    async startCapturing() {
        if (this.isCapturing) {
            return; // 已經在錄製中，不重複啟動
        }

        try {
            // 清空截圖集合
            this.capturedImages = [];
            
            // 請求使用者授權捕獲螢幕
            this.stream = await navigator.mediaDevices.getDisplayMedia({
                video: {
                    cursor: "always"
                },
                audio: false
            });
            
            // 創建隱藏的 video 元素，用於接收媒體流
            this.videoElement = document.createElement('video');
            this.videoElement.srcObject = this.stream;
            this.videoElement.autoplay = true;
            this.videoElement.style.display = 'none';
            document.body.appendChild(this.videoElement);
            
            // 等待視訊加載完成
            await new Promise(resolve => {
                this.videoElement.onloadedmetadata = () => {
                    this.videoElement.play();
                    resolve();
                };
            });
            
            // 立即執行一次截圖
            await this.captureScreen();
            
            // 設置定時器，定期截圖
            const frequency = this.settingsService.getCaptureFrequencyInMilliseconds();
            this.captureTimer = setInterval(() => this.captureScreen(), frequency);
            
            // 監聽流的結束事件
            this.stream.getVideoTracks()[0].onended = () => {
                this.stopCapturing();
            };
            
            this.isCapturing = true;
        } catch (error) {
            console.error('Error starting screen capture:', error);
            this.cleanupResources();
            throw new Error(`無法啟動螢幕錄製: ${error.message}`);
        }
    }

    /**
     * 清理資源
     */
    cleanupResources() {
        // 停止並清理媒體流
        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
        }
        
        // 移除 video 元素
        if (this.videoElement) {
            document.body.removeChild(this.videoElement);
            this.videoElement = null;
        }
    }

    /**
     * 停止螢幕錄製
     */
    stopCapturing() {
        if (!this.isCapturing) {
            return;
        }

        // 停止定時器
        if (this.captureTimer) {
            clearInterval(this.captureTimer);
            this.captureTimer = null;
        }
        
        // 清理資源
        this.cleanupResources();
        
        // 停止時清空截圖集合
        this.capturedImages = [];
        
        this.isCapturing = false;
    }

    /**
     * 檢查是否正在錄製
     * @returns {boolean}
     */
    isRecording() {
        return this.isCapturing;
    }

    /**
     * 提交當前收集的截圖批次
     */
    submitCapturedBatch() {
        if (this.capturedImages.length === 0) {
            return;
        }
        
        // 創建截圖集合的副本
        const imagesToSubmit = [...this.capturedImages];
        
        // 觸發事件，傳送截圖集合
        this.triggerScreensCaptureBatch(imagesToSubmit);
        
        // 清空截圖集合，準備收集下一批
        this.capturedImages = [];
    }
}

// 建立螢幕錄製服務的實例
const screenCaptureService = new ScreenCaptureService(settingsService); 