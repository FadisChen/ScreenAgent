/**
 * 語音服務模組 - 負責文字轉語音功能
 */
class SpeechService {
    /**
     * 建立語音服務
     * @param {SettingsService} settingsService - 設定服務實例
     */
    constructor(settingsService) {
        this.settingsService = settingsService;
        this.currentVoice = '';
        this.utterance = null;
        this.voices = [];
        
        // 初始化語音合成
        this.initSpeechSynthesis();
    }

    /**
     * 初始化語音合成，獲取可用的語音
     */
    initSpeechSynthesis() {
        // 檢查瀏覽器是否支援語音合成
        if (!'speechSynthesis' in window) {
            console.warn('瀏覽器不支援語音合成功能');
            return;
        }

        // 獲取所有可用語音
        this.loadVoices();
        
        // 當語音列表可用時更新列表
        if (window.speechSynthesis.onvoiceschanged !== undefined) {
            window.speechSynthesis.onvoiceschanged = this.loadVoices.bind(this);
        }
    }

    /**
     * 載入可用的語音
     */
    loadVoices() {
        // 獲取所有可用語音
        this.voices = window.speechSynthesis.getVoices();
        
        // 更新當前語音設定
        this.updateVoiceSetting();
    }

    /**
     * 更新語音設定
     */
    updateVoiceSetting() {
        try {
            // 從設定服務獲取語音設定
            const voiceName = this.settingsService.getTextToSpeechVoiceName();
            
            if (voiceName) {
                // 查找指定的語音
                this.currentVoice = voiceName;
            } else {
                // 如果沒有設定，嘗試使用中文語音
                const chineseVoice = this.voices.find(v => 
                    v.lang === 'zh-TW');
                
                if (chineseVoice) {
                    this.currentVoice = chineseVoice.name;
                } else if (this.voices.length > 0) {
                    // 如果沒有中文語音，使用第一個可用的語音
                    this.currentVoice = this.voices[0].name;
                }
            }
        } catch (error) {
            console.error('更新語音設定時發生錯誤:', error);
        }
    }

    /**
     * 獲取所有可用的中文語音
     * @returns {Array} 可用語音清單
     */
    getAvailableVoices() {
        // 返回所有中文語音，如果沒有中文語音，則返回所有語音
        const chineseVoices = this.voices.filter(v => 
            v.lang === 'zh-TW');
        
        return chineseVoices.length > 0 ? chineseVoices : this.voices;
    }

    /**
     * 朗讀文字
     * @param {string} text - 要朗讀的文字
     */
    speak(text) {
        // 檢查是否啟用語音功能
        if (!this.settingsService.getEnableTextToSpeech()) {
            return;
        }

        try {
            // 停止先前的朗讀
            this.stop();
            
            // 更新語音設定
            this.updateVoiceSetting();
            
            // 創建語音設定
            this.utterance = new SpeechSynthesisUtterance(text);
            
            // 設定語音
            const voice = this.voices.find(v => v.name === this.currentVoice) || 
                this.voices.find(v => v.lang === 'zh-TW') ||
                this.voices[0];
            
            if (voice) {
                this.utterance.voice = voice;
                this.utterance.lang = voice.lang;
                console.log(`使用語音: ${voice.name} (${voice.lang})`);
            }
            
            // 設定語音速率和音高
            this.utterance.rate = 1.0;
            this.utterance.pitch = 1.0;
            
            // 開始播放語音
            window.speechSynthesis.speak(this.utterance);
        } catch (error) {
            console.error('播放語音時發生錯誤:', error);
        }
    }

    /**
     * 暫停語音播放
     */
    pause() {
        window.speechSynthesis.pause();
    }

    /**
     * 恢復語音播放
     */
    resume() {
        window.speechSynthesis.resume();
    }

    /**
     * 停止語音播放
     */
    stop() {
        window.speechSynthesis.cancel();
        this.utterance = null;
    }

    /**
     * 檢查是否正在播放語音
     * @returns {boolean}
     */
    isSpeaking() {
        return window.speechSynthesis.speaking;
    }
}

// 建立語音服務的實例
const speechService = new SpeechService(settingsService); 