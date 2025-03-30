using Edge_tts_sharp;
using Edge_tts_sharp.Model;
using Edge_tts_sharp.Utils;

namespace ScreenAgent.Services
{
    public class TextToSpeechService
    {
        private static AudioPlayer? _audioPlayer;
        private readonly SettingsService? _settingsService;
        private string _currentVoice = "zh-TW-HsiaoChenNeural";
        private CancellationTokenSource? _speechCancellationTokenSource;
        
        public TextToSpeechService(SettingsService? settingsService = null)
        {
            _settingsService = settingsService;

            // 初始設定
            UpdateVoiceSetting();
        }

        // 更新語音設定，從 SettingsService 獲取最新設定
        private void UpdateVoiceSetting()
        {
            try
            {
                if (_settingsService != null)
                {
                    var voiceName = _settingsService.GetTextToSpeechVoiceName();
                    if (!string.IsNullOrEmpty(voiceName))
                    {
                        _currentVoice = voiceName;
                    }
                    else
                    {
                        // 如果沒有設定，則使用預設台灣語音
                        _currentVoice = "zh-TW-HsiaoChenNeural";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating voice setting: {ex.Message}");
            }
        }

        public IEnumerable<string> GetAvailableVoices()
        {
            try
            {
                // 獲取所有可用的語音
                var voices = Edge_tts.GetVoice();

                // 只返回 zh-TW 開頭的語音名稱
                return voices
                    .Where(v => v.ShortName?.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(v => v.ShortName)
                    .OrderBy(name => name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting available voices: {ex.Message}");
                return new List<string>();
            }
        }

        public void Speak(string text)
        {
            // 檢查是否啟用TTS
            if (_settingsService != null && !_settingsService.GetEnableTextToSpeech())
            {
                return;
            }

            try
            {
                // 在每次播放前更新語音設定
                UpdateVoiceSetting();

                // 停止先前的語音播放
                Stop();

                // 創建新的取消令牌源
                _speechCancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _speechCancellationTokenSource.Token;

                // 非同步說話，不阻塞UI線程
                Task.Run(() =>
                {
                    try
                    {
                        // 建立播放選項
                        PlayOption option = new PlayOption
                        {
                            Rate = 0,  // 正常速度
                            Text = text
                        };

                        // 取得所有語音
                        var voices = Edge_tts.GetVoice();

                        // 尋找指定的語音
                        var voice = voices.FirstOrDefault(v => 
                            v.ShortName != null && 
                            v.ShortName.Equals(_currentVoice, StringComparison.OrdinalIgnoreCase)) 
                            ?? 
                            // 找不到指定語音時，嘗試使用預設的台灣語音
                            voices.FirstOrDefault(v => 
                                v.ShortName != null && 
                                v.ShortName.Equals("zh-TW-HsiaoChenNeural", StringComparison.OrdinalIgnoreCase)) 
                            ?? 
                            // 如果還是找不到，使用任何可用的第一個語音
                            voices.First();

                        Console.WriteLine($"正在使用語音: {voice.ShortName}");

                        _audioPlayer = Edge_tts.GetPlayer(option, voice);
                        _audioPlayer.PlayAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("語音播放已取消");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in TTS thread: {ex.Message}");
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error speaking text: {ex.Message}");
            }
        }

        public void Stop()
        {
            _audioPlayer?.Stop();
        }
    }
}