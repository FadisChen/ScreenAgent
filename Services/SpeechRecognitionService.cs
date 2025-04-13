using ScreenAgent.Models;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Speech.Recognition;
using System.Globalization;
using System.Linq;

namespace ScreenAgent.Services
{
    public class SpeechRecognitionService
    {
        private readonly SettingsService _settingsService;
        private SpeechRecognitionEngine _recognizer;
        private bool _isListening = false;

        // 語音識別狀態變更事件
        public event EventHandler<bool> RecognitionStateChanged;
        
        // 識別結果事件
        public event EventHandler<string> SpeechRecognized;

        public SpeechRecognitionService(SettingsService settingsService)
        {
            _settingsService = settingsService;
            InitializeSpeechRecognition();
        }

        private void InitializeSpeechRecognition()
        {
            try
            {
                // 取得系統使用的語言文化
                string cultureName = CultureInfo.CurrentUICulture.Name;
                CultureInfo cultureInfo;

                // 嘗試使用系統文化，如果不支援則使用美式英文
                try
                {
                    cultureInfo = new CultureInfo(cultureName);
                    if (!SpeechRecognitionEngine.InstalledRecognizers().Cast<RecognizerInfo>()
                        .Any(r => r.Culture.Equals(cultureInfo)))
                    {
                        // 如果系統語言不支援，使用美式英文
                        cultureInfo = new CultureInfo("en-US");
                    }
                }
                catch
                {
                    // 出錯時使用美式英文
                    cultureInfo = new CultureInfo("en-US");
                }

                // 建立語音識別引擎
                _recognizer = new SpeechRecognitionEngine(cultureInfo);

                // 設定語法 - 使用聽寫語法，可以識別自由形式的語音
                _recognizer.LoadGrammar(new DictationGrammar());

                // 註冊事件
                _recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
                _recognizer.RecognizeCompleted += Recognizer_RecognizeCompleted;

                // 設定音訊輸入
                _recognizer.SetInputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"初始化語音識別失敗：{ex.Message}", "語音識別錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                _recognizer = null;
            }
        }

        public bool IsAvailable()
        {
            return _recognizer != null;
        }

        public bool IsListening()
        {
            return _isListening;
        }

        public void StartListening()
        {
            if (_recognizer != null && !_isListening)
            {
                try
                {
                    _recognizer.RecognizeAsync(RecognizeMode.Multiple);
                    _isListening = true;
                    RecognitionStateChanged?.Invoke(this, true);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"啟動語音識別失敗：{ex.Message}", "語音識別錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void StopListening()
        {
            if (_recognizer != null && _isListening)
            {
                try
                {
                    _recognizer.RecognizeAsyncStop();
                    _isListening = false;
                    RecognitionStateChanged?.Invoke(this, false);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"停止語音識別失敗：{ex.Message}", "語音識別錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && e.Result.Text.Length > 0)
            {
                // 觸發事件，將識別的文字發送給訂閱者
                SpeechRecognized?.Invoke(this, e.Result.Text);
            }
        }

        private void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            // 如果不是被手動停止，則可能是錯誤導致停止
            if (!e.Cancelled && e.Error != null)
            {
                System.Windows.MessageBox.Show($"語音識別發生錯誤：{e.Error.Message}", "語音識別錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _isListening = false;
            RecognitionStateChanged?.Invoke(this, false);
        }
    }
} 