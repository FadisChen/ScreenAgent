using System;
using System.Windows;
using System.Linq;
using ScreenAgent.Services;

namespace ScreenAgent.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private readonly TextToSpeechService _textToSpeechService;

        public SettingsWindow(SettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _textToSpeechService = new TextToSpeechService(_settingsService);
            
            // 載入設定
            LoadSettings();
        }

        private void LoadSettings()
        {
            // API Key
            txtApiKey.Password = _settingsService.GetApiKey();
            
            // Gemini 模型
            txtGeminiModel.Text = _settingsService.GetGeminiModel();
            
            // 顯示對話歷史
            chkShowHistory.IsChecked = _settingsService.GetShowConversationHistory();
            
            // 截圖頻率
            int frequencyInSeconds = _settingsService.GetCaptureFrequencyInMilliseconds() / 1000;
            sliderFrequency.Value = frequencyInSeconds;
            txtFrequency.Text = frequencyInSeconds.ToString();
            
            // 啟用文字轉語音
            chkEnableTTS.IsChecked = _settingsService.GetEnableTextToSpeech();
            
            // 語音識別相關設定
            chkEnableSTT.IsChecked = _settingsService.GetEnableSpeechToText();
            
            // 發送訊息時截圖
            chkCaptureOnSend.IsChecked = _settingsService.GetCaptureOnSend();
            
            // 載入 System Prompt
            txtSystemPrompt.Text = _settingsService.GetSystemPrompt();
            
            // 載入可用的聲音
            LoadAvailableVoices();
        }

        private void LoadAvailableVoices()
        {
            try
            {
                var voices = _textToSpeechService.GetAvailableVoices().ToList();
                cmbVoices.ItemsSource = voices;
                
                // 選擇目前設定的聲音
                var currentVoice = _settingsService.GetTextToSpeechVoiceName();
                if (!string.IsNullOrEmpty(currentVoice) && voices.Contains(currentVoice))
                {
                    cmbVoices.SelectedItem = currentVoice;
                }
                else if (voices.Any())
                {
                    cmbVoices.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"載入語音列表時出錯: {ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SliderFrequency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtFrequency != null)
            {
                int value = (int)e.NewValue;
                txtFrequency.Text = value.ToString();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 開始批次更新設定
            _settingsService.BeginUpdate();
            
            // 儲存所有設定值
            _settingsService.SetApiKey(txtApiKey.Password);
            _settingsService.SetGeminiModel(txtGeminiModel.Text);
            _settingsService.SetShowConversationHistory(chkShowHistory.IsChecked ?? true);
            _settingsService.SetCaptureFrequencyInSeconds((int)sliderFrequency.Value);
            _settingsService.SetEnableTextToSpeech(chkEnableTTS.IsChecked ?? true);
            
            // 儲存語音識別相關設定
            _settingsService.SetEnableSpeechToText(chkEnableSTT.IsChecked ?? true);
            
            // 儲存發送訊息時截圖設定
            _settingsService.SetCaptureOnSend(chkCaptureOnSend.IsChecked ?? true);
            
            // 儲存 System Prompt
            _settingsService.SetSystemPrompt(txtSystemPrompt.Text);
            
            string selectedVoice = cmbVoices.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedVoice))
            {
                _settingsService.SetTextToSpeechVoiceName(selectedVoice);
            }
            
            // 完成批次更新，一次性保存所有設定
            _settingsService.EndUpdate();
            
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
    }
}