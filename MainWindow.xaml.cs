using ScreenAgent.Services;
using ScreenAgent.Views;
using System.Windows;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScreenAgent.Models;

namespace ScreenAgent;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly ScreenCaptureService _screenCaptureService;
    private readonly GeminiService _geminiService;
    private readonly TextToSpeechService _speechService;
    private readonly SpeechRecognitionService _speechRecognitionService;
    private bool _isCapturing = false;
    private ConversationWindow _historyWindow;

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化服務
        _settingsService = new SettingsService();
        _screenCaptureService = new ScreenCaptureService(_settingsService);
        _geminiService = new GeminiService(_settingsService);
        _speechService = new TextToSpeechService(_settingsService);
        _speechRecognitionService = new SpeechRecognitionService(_settingsService);
        
        // 創建對話歷史視窗（但不顯示）
        _historyWindow = new ConversationWindow();
        
        // 註冊事件
        _screenCaptureService.ScreensCaptureBatch += OnScreensCaptureBatch;
        _speechRecognitionService.SpeechRecognized += OnSpeechRecognized;
        _speechRecognitionService.RecognitionStateChanged += OnRecognitionStateChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 將視窗移動到螢幕底部中央
        PositionWindowAtBottomCenter();
        
        // 建立對話歷史視窗
        _historyWindow.Show();
        _historyWindow.PositionWindowAtRightSide();
        
        // 根據設定決定是否顯示對話歷史視窗
        UpdateHistoryWindowVisibility();
        
        // 設定文字框按 Enter 鍵提交
        txtPrompt.KeyDown += TxtPrompt_KeyDown;
    }

    private void PositionWindowAtBottomCenter()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;
        
        this.Left = (screenWidth - this.Width) / 2;
        this.Top = screenHeight - this.Height - 100;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void BtnStartStop_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturing)
        {
            StopCapturing();
        }
        else
        {
            StartCapturing();
        }
    }
    
    private void StartCapturing()
    {
        // 檢查API Key是否已設定
        if (string.IsNullOrEmpty(_settingsService.GetApiKey()))
        {
            System.Windows.MessageBox.Show("請先在設定中設定您的 Gemini API Key。", 
                            "未設定 API Key", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Warning);
            OpenSettingsWindow();
            return;
        }
        
        try
        {
            // 先中斷正在播放的語音
            _speechService.Stop();
            
            // 強制隱藏對話視窗，不考慮設定值
            if (_historyWindow != null)
            {
                _historyWindow.Visibility = Visibility.Collapsed;
            }
            
            _screenCaptureService.StartCapturing();
            _isCapturing = true;
            
            // 切換圖標 - 顯示停止圖標，隱藏開始圖標
            iconRecord.Visibility = Visibility.Collapsed;
            iconStop.Visibility = Visibility.Visible;
            
            // 提示用戶截圖已開始
            SetStatus("影像輔助模式已開啟，請輸入問題並按 Enter 鍵發送查詢");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"啟動影像輔助失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
        }
    }
    
    private void StopCapturing()
    {
        try
        {
            _screenCaptureService.StopCapturing();
            _isCapturing = false;
            
            // 切換圖標 - 顯示開始圖標，隱藏停止圖標
            iconRecord.Visibility = Visibility.Visible;
            iconStop.Visibility = Visibility.Collapsed;
            
            // 根據設定重新決定對話視窗顯示狀態
            UpdateHistoryWindowVisibility();
            
            // 更新狀態
            SetStatus("影像輔助模式已關閉");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"停止影像輔助失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
        }
    }
    
    private async void OnScreensCaptureBatch(object sender, List<byte[]> imageBytesList)
    {            
        try
        {
            string userPrompt = txtPrompt.Text.Trim();
            
            // 顯示處理中的消息
            SetStatus($"處理中... ({imageBytesList.Count} 張截圖)");
            
            // 處理包含截圖的請求
            await ProcessGeminiRequestAsync(imageBytesList, userPrompt);
            
            // 清空操作已經在 ProcessGeminiRequestAsync 中完成
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"處理失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
            SetStatus("發生錯誤，請重試");
        }
    }
    
    private async Task ProcessGeminiRequestAsync(List<byte[]> imageBytesList, string userPrompt)
    {
        try
        {
            // 調用 Gemini API
            string response = await _geminiService.GetResponseFromImageBatchAndTextAsync(imageBytesList, userPrompt);
            response = response.Replace("**","");
            
            _historyWindow.DisplayResponse(userPrompt, response);
            
            // 根據設定決定是否顯示對話歷史視窗
            UpdateHistoryWindowVisibility();
            
            // 文字轉語音
            _speechService.Speak(response);
            
            // 更新狀態
            SetStatus("已完成回應");
            
            // 確保截圖列表已清空
            _screenCaptureService.ClearCapturedImages();
        }
        catch (Exception ex)
        {
            // 發生錯誤時顯示訊息
            System.Windows.MessageBox.Show($"處理請求失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
            
            SetStatus("處理請求發生錯誤");
            throw; // 重新拋出例外以便上層處理
        }
    }

    private void SetStatus(string message)
    {
        // 更新窗口標題
        this.Title = $"Screen Agent - {message}";
        
        // 更新狀態文字區域
        txtStatus.Text = message;
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }
    
    private void OpenSettingsWindow()
    {
        // 如果正在捕捉，暫停
        bool wasCapturing = _isCapturing;
        if (wasCapturing)
        {
            StopCapturing();
        }
        
        var settingsWindow = new SettingsWindow(_settingsService);
        bool? result = settingsWindow.ShowDialog();
        
        if (result == true)
        {
            // 設定已保存，更新對話歷史視窗的可見性
            UpdateHistoryWindowVisibility();
            
            // 如果之前在捕捉，恢復捕捉
            if (wasCapturing)
            {
                StartCapturing();
            }
        }
    }
    
    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        // 顯示確認對話框
        MessageBoxResult result = System.Windows.MessageBox.Show(
            "確定要關閉應用程式嗎？",
            "確認關閉",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
            
        if (result == MessageBoxResult.Yes)
        {
            // 關閉應用程式
            System.Windows.Application.Current.Shutdown();
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        // 確保停止截圖
        if (_isCapturing)
        {
            _screenCaptureService.StopCapturing();
        }
        
        // 關閉對話歷史視窗
        _historyWindow.Close();
        
        base.OnClosed(e);
    }

    private void UpdateHistoryWindowVisibility()
    {
        // 根據設定決定是否顯示對話歷史視窗
        bool showHistory = _settingsService.GetShowConversationHistory();
        
        // 更新視窗可見性
        if (_historyWindow != null)
        {
            _historyWindow.Visibility = showHistory ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void TxtPrompt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 按下 Enter 鍵提交
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;  // 防止 Enter 鍵換行
            
            // 相當於點擊提交按鈕，使用非同步方式呼叫
            _ = SubmitQueryAsync();
        }
    }
    
    private async Task SubmitQueryAsync()
    {
        // 取得輸入文字
        string userPrompt = txtPrompt.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(userPrompt))
            return;
            
        // 中斷任何正在播放的語音
        _speechService.Stop();

        //如果麥克風已開啟，需關閉麥克風
        if (_speechRecognitionService.IsListening())
        {
            _speechRecognitionService.StopListening();
        }

        SetStatus("正在處理您的請求...");
        
        if (_isCapturing)
        {
            // 提交當前收集的截圖，並停止截圖
            _screenCaptureService.SubmitCapturedBatch();
            
            // 停止截圖模式
            _screenCaptureService.StopCapturing();
            _isCapturing = false;
            
            // 還原按鈕圖示
            iconRecord.Visibility = Visibility.Visible;
            iconStop.Visibility = Visibility.Collapsed;
            
            // 根據設定恢復對話視窗顯示狀態
            UpdateHistoryWindowVisibility();
        }
        else
        {
            // 檢查發送訊息時截圖的設定
            bool captureOnSend = _settingsService.GetCaptureOnSend();
            
            if (captureOnSend)
            {
                try
                {
                    // 暫時隱藏對話視窗以便乾淨截圖
                    bool originalVisibility = _historyWindow.Visibility == Visibility.Visible;
                    if (_historyWindow != null)
                    {
                        _historyWindow.Visibility = Visibility.Collapsed;
                    }
                    
                    try
                    {
                        // 短暫延遲以確保UI已完全更新
                        await Task.Delay(100);
                        
                        // 擷取一次螢幕截圖
                        var imageBytes = _screenCaptureService.CaptureScreenSingle();
                        List<byte[]> imageList = new List<byte[]> { imageBytes };
                        
                        // 使用包含截圖的方式處理請求
                        await ProcessGeminiRequestAsync(imageList, userPrompt);
                    }
                    finally
                    {
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"截圖失敗: {ex.Message}", 
                                    "錯誤", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Error);
                    
                    // 使用純文字查詢作為後備方案
                    SubmitTextOnlyQuery(userPrompt);
                }
            }
            else
            {
                // 如果沒有在截圖，則直接提交文本查詢
                SubmitTextOnlyQuery(userPrompt);
            }
        }
        
        // 清空輸入框
        txtPrompt.Text = string.Empty;
    }

    private async void SubmitTextOnlyQuery(string userPrompt)
    {
        try
        {
            SetStatus("處理中...");

            // 使用新的純文字回應方法而不是包含空圖片列表的多模態方法
            string response = await _geminiService.GetResponseFromTextAsync(userPrompt);
            response = response.Replace("**","");
            
            _historyWindow.DisplayResponse(userPrompt, response);
            
            // 根據設定決定是否顯示對話歷史視窗
            UpdateHistoryWindowVisibility();
            
            // 文字轉語音
            _speechService.Speak(response);
            
            // 更新狀態
            SetStatus("已完成回應");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"處理失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
            SetStatus("發生錯誤，請重試");
        }
    }

    private void OnSpeechRecognized(object sender, string recognizedText)
    {
        // 在UI線程上更新TextBox
        this.Dispatcher.Invoke(() =>
        {
            // 將辨識到的文字添加到目前輸入框的文字後
            txtPrompt.Text += (txtPrompt.Text.Length > 0 ? " " : "") + recognizedText;
            txtPrompt.CaretIndex = txtPrompt.Text.Length; // 將游標移至末尾
            txtPrompt.Focus();
        });
    }

    private void OnRecognitionStateChanged(object sender, bool isListening)
    {
        // 在UI線程上更新麥克風圖標
        this.Dispatcher.Invoke(() =>
        {
            iconMic.Visibility = isListening ? Visibility.Collapsed : Visibility.Visible;
            iconMicRecording.Visibility = isListening ? Visibility.Visible : Visibility.Collapsed;
            
            // 更新狀態文字
            if (isListening)
            {
                SetStatus("正在聆聽，請說話...");
            }
            else
            {
                SetStatus("語音輸入已停止");
            }
        });
    }

    private void BtnMicrophone_Click(object sender, RoutedEventArgs e)
    {
        if (!_speechRecognitionService.IsAvailable())
        {
            System.Windows.MessageBox.Show("語音識別功能不可用，請確認系統已安裝必要的語音套件。", 
                "語音辨識錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (_speechRecognitionService.IsListening())
        {
            _speechRecognitionService.StopListening();
        }
        else
        {
            _speechRecognitionService.StartListening();
        }
    }
}