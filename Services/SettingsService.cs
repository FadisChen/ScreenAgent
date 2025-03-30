using ScreenAgent.Models;
using Newtonsoft.Json;
using System.IO;

namespace ScreenAgent.Services
{
    public class SettingsService
    {
        private readonly string _configPath;
        private AppSettings _settings;
        private bool _autoSave = true;

        public SettingsService(bool autoSave = true)
        {
            _autoSave = autoSave;
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScreenAgent",
                "settings.json");
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json);
                }
                else
                {
                    _settings = new AppSettings
                    {
                        ApiKey = "",
                        ShowConversationHistory = true,
                        CaptureFrequencyInSeconds = 1
                    };
                    SaveSettings();
                }
            }
            catch
            {
                _settings = new AppSettings
                {
                    ApiKey = "",
                    ShowConversationHistory = true,
                    CaptureFrequencyInSeconds = 1
                };
            }
        }
        
        public void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        public string GetApiKey() => _settings.ApiKey;
        
        public bool GetShowConversationHistory() => _settings.ShowConversationHistory;
        
        public int GetCaptureFrequencyInMilliseconds() => _settings.CaptureFrequencyInSeconds * 1000;
        
        public string GetTextToSpeechVoiceName() => _settings.TextToSpeechVoiceName;

        public string GetGeminiModel() => _settings.GeminiModel;

        public bool GetEnableTextToSpeech() => _settings.EnableTextToSpeech;
        
        // 修改設定值的方法，增加不自動保存的選項
        private void SetValue<T>(Action<T> setter, T value)
        {
            setter(value);
            if (_autoSave)
            {
                SaveSettings();
            }
        }
        
        public void SetApiKey(string key) => SetValue(k => _settings.ApiKey = k, key);
        
        public void SetShowConversationHistory(bool show) => SetValue(s => _settings.ShowConversationHistory = s, show);
        
        public void SetCaptureFrequencyInSeconds(int seconds) => SetValue(s => _settings.CaptureFrequencyInSeconds = s, seconds);
        
        public void SetTextToSpeechVoiceName(string name) => SetValue(n => _settings.TextToSpeechVoiceName = n, name);
        
        public void SetGeminiModel(string model) => SetValue(m => _settings.GeminiModel = m, model);
        
        public void SetEnableTextToSpeech(bool enable) => SetValue(e => _settings.EnableTextToSpeech = e, enable);
        
        // 批次更新設定而不是每次都保存
        public void BeginUpdate()
        {
            _autoSave = false;
        }
        
        public void EndUpdate()
        {
            _autoSave = true;
            SaveSettings();
        }

        public string GetSystemPrompt() => _settings.SystemPrompt;
        
        public void SetSystemPrompt(string prompt) => SetValue(p => _settings.SystemPrompt = p, prompt);
    }
}