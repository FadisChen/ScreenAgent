using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Threading;
using System.Speech.Recognition;
using System.Globalization;
using System.Runtime.InteropServices;
using NAudio.Wave;

namespace ScreenAgent.Services
{
    public class SpeechToTextService
    {
        private readonly SettingsService _settingsService;
        private SpeechRecognitionEngine _recognizer;
        private bool _isListening = false;
        private CancellationTokenSource _cancellationTokenSource;
        
        // 語音識別結果事件
        public event EventHandler<string> SpeechRecognized;
        
        // 語音開始/結束事件
        public event EventHandler SpeechStarted;
        public event EventHandler SpeechEnded;
        
        // VAD 靜音事件（檢測到一段靜音後觸發）
        public event EventHandler SilenceDetected;
        
        // 參數設定
        private const int SILENCE_TIMEOUT_MS = 2000; // 2秒沒有語音視為靜音
        private bool _hasDetectedSpeech = false; // 是否已經偵測到過語音
        private DateTime _startListeningTime; // 開始聆聽的時間
        private bool _showedNoSignalWarning = false; // 是否已顯示過無信號警告
        
        public SpeechToTextService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        public bool IsListening => _isListening;

        // 檢查麥克風是否可用
        private bool IsMicrophoneAvailable()
        {
            try
            {
                // 使用 NAudio 檢查麥克風設備
                int waveInDevices = WaveIn.DeviceCount;
                if (waveInDevices == 0)
                {
                    return false;
                }

                // 嘗試打開一個麥克風設備來確認是否真的可用
                using (var waveIn = new WaveInEvent())
                {
                    waveIn.DeviceNumber = 0;
                    waveIn.WaveFormat = new WaveFormat(16000, 1);
                    
                    // 只確認裝置是否存在，不實際開始錄音
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        
        public async Task StartListeningAsync()
        {
            if (_isListening)
                return;
                
            try
            {
                // 檢查麥克風是否可用
                if (!IsMicrophoneAvailable())
                {
                    throw new Exception("找不到可用的麥克風設備，請確認麥克風已正確連接並啟用。");
                }
                
                // 獲取語言設定
                string speechLanguage = _settingsService.GetSpeechLanguage();
                
                // 創建語音識別引擎
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(speechLanguage);
                }
                catch
                {
                    // 預設使用繁體中文
                    culture = new CultureInfo("zh-TW");
                }
                
                try
                {
                    _recognizer = new SpeechRecognitionEngine(culture);
                }
                catch (Exception ex)
                {
                    throw new Exception($"無法初始化語音識別，可能不支援 {culture.DisplayName} 語言: {ex.Message}");
                }
                
                // 載入語法
                try
                {
                    // 添加聽寫語法
                    var dictationGrammar = new DictationGrammar();
                    dictationGrammar.Name = "dictation";
                    _recognizer.LoadGrammar(dictationGrammar);
                }
                catch (Exception ex)
                {
                    throw new Exception($"載入語音語法失敗: {ex.Message}");
                }
                
                // 設定識別引擎參數 - 增加初始靜音容忍度
                try
                {
                    // 設定初始靜音容忍時間
                    _recognizer.InitialSilenceTimeout = TimeSpan.FromMilliseconds(SILENCE_TIMEOUT_MS);
                    // 設定語音間靜音識別時間
                    _recognizer.BabbleTimeout = TimeSpan.FromMilliseconds(SILENCE_TIMEOUT_MS);
                    // 設定最短語音長度
                    _recognizer.EndSilenceTimeout = TimeSpan.FromMilliseconds(500);
                    // 設定最長識別時間
                    _recognizer.EndSilenceTimeoutAmbiguous = TimeSpan.FromMilliseconds(700);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"設定識別引擎參數失敗: {ex.Message}");
                    // 不中斷流程，使用默認參數繼續
                }
                
                // 註冊事件
                _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;
                _recognizer.AudioSignalProblemOccurred += Recognizer_AudioSignalProblemOccurred;
                _recognizer.SpeechDetected += Recognizer_SpeechDetected;
                
                // 設定音訊輸入（使用更友好的錯誤處理）
                try
                {
                    _recognizer.SetInputToDefaultAudioDevice();
                }
                catch (Exception ex)
                {
                    throw new Exception($"設定音訊輸入失敗，請確認麥克風已正確連接: {ex.Message}");
                }
                
                // 重置狀態
                _hasDetectedSpeech = false;
                _showedNoSignalWarning = false;
                _startListeningTime = DateTime.Now;
                
                // 清除取消令牌
                _cancellationTokenSource = new CancellationTokenSource();
                
                // 開始異步識別
                try
                {
                    _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    _isListening = true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"開始語音識別失敗: {ex.Message}");
                }
                
                // 啟動 VAD 監控
                StartVadMonitoring();
                
                // 觸發開始事件
                SpeechStarted?.Invoke(this, EventArgs.Empty);
                
                // 由於已經完成初始化，可以立即返回
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // 確保釋放任何已分配的資源
                if (_recognizer != null)
                {
                    try
                    {
                        _recognizer.Dispose();
                        _recognizer = null;
                    }
                    catch { }
                }
                
                System.Windows.MessageBox.Show($"啟動語音識別失敗: {ex.Message}",
                                "錯誤",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                throw; // 重新拋出例外，讓呼叫者知道發生了錯誤
            }
        }
        
        public async Task StopListeningAsync()
        {
            if (!_isListening || _recognizer == null)
                return;
                
            try
            {
                // 取消 VAD 監控
                _cancellationTokenSource.Cancel();
                
                // 停止識別
                _recognizer.RecognizeAsyncStop();
                _isListening = false;
                
                // 解除事件註冊
                _recognizer.SpeechRecognized -= Recognizer_SpeechRecognized;
                _recognizer.RecognizeCompleted -= Recognizer_RecognizeCompleted;
                _recognizer.AudioSignalProblemOccurred -= Recognizer_AudioSignalProblemOccurred;
                _recognizer.SpeechDetected -= Recognizer_SpeechDetected;
                
                // 釋放資源
                _recognizer.Dispose();
                _recognizer = null;
                
                // 觸發結束事件
                SpeechEnded?.Invoke(this, EventArgs.Empty);
                
                // 函數已完成
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"停止語音識別失敗: {ex.Message}",
                                "錯誤",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
        
        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null)
            {
                _hasDetectedSpeech = true;
                string recognizedText = e.Result.Text;
                SpeechRecognized?.Invoke(this, recognizedText);
                
                // 重置 VAD 靜音計時器
                ResetSilenceTimer();
            }
        }
        
        private void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Windows.MessageBox.Show($"語音識別錯誤: {e.Error.Message}",
                                "語音識別錯誤",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            
            if (e.Cancelled || e.InitialSilenceTimeout)
            {
                _isListening = false;
                SpeechEnded?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void Recognizer_AudioSignalProblemOccurred(object sender, AudioSignalProblemOccurredEventArgs e)
        {
            // 改為記錄訊息而不顯示彈窗
            Console.WriteLine($"音訊信號問題: {e.AudioSignalProblem}");
            
            // 如果是沒有信號，且等待足夠長的時間後才顯示警告
            if (e.AudioSignalProblem == AudioSignalProblem.NoSignal && 
                !_showedNoSignalWarning && 
                DateTime.Now - _startListeningTime > TimeSpan.FromSeconds(3))
            {
                // 設置標記，避免重複顯示
                _showedNoSignalWarning = true;
                
                // 先停止目前的識別
                _isListening = false;
                
                // 提示使用者檢查麥克風
                System.Windows.MessageBox.Show(
                    "無法偵測到麥克風信號，請檢查您的麥克風連接和系統設定，然後再試一次。",
                    "麥克風信號問題",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                // 觸發結束事件
                SpeechEnded?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void Recognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            // 設置已偵測到語音的標記
            _hasDetectedSpeech = true;
            
            // 當檢測到語音時，重置靜音計時器
            ResetSilenceTimer();
        }
        
        #region VAD (Voice Activity Detection) Implementation
        
        private DateTime _lastSpeechTime = DateTime.MinValue;
        private System.Threading.Timer _silenceTimer;
        
        private void StartVadMonitoring()
        {
            // 初始化上次說話時間
            _lastSpeechTime = DateTime.Now;
            
            // 建立靜音檢測計時器 - 較長的檢查間隔，降低靈敏度
            _silenceTimer = new System.Threading.Timer(CheckSilence, null, 2000, 2000);
        }
        
        private void ResetSilenceTimer()
        {
            _lastSpeechTime = DateTime.Now;
        }
        
        private void CheckSilence(object state)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;
                
            TimeSpan timeSinceLastSpeech = DateTime.Now - _lastSpeechTime;
            
            // 如果超過設定的靜音時間，且已經曾經檢測到語音，才觸發靜音事件
            if (timeSinceLastSpeech.TotalMilliseconds > SILENCE_TIMEOUT_MS && _hasDetectedSpeech)
            {
                SilenceDetected?.Invoke(this, EventArgs.Empty);
                
                // 停止計時器
                _silenceTimer?.Dispose();
                _silenceTimer = null;
            }
            // 如果開始後很長時間沒有語音，也不需要顯示提示
            else if (!_hasDetectedSpeech && DateTime.Now - _startListeningTime > TimeSpan.FromSeconds(30))
            {
                // 超過30秒仍未檢測到語音，自動停止但不顯示錯誤
                SilenceDetected?.Invoke(this, EventArgs.Empty);
                
                // 停止計時器
                _silenceTimer?.Dispose();
                _silenceTimer = null;
            }
        }
        
        #endregion
    }
} 