using ScreenAgent.Models;
using ScreenAgent.Services;
using ScreenAgent.Views;
using System.Windows;
using System.Windows.Input;

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
    private readonly SpeechToTextService _speechToTextService;
    private bool _isCapturing = false;
    private bool _isListening = false;
    private ConversationHistoryWindow _historyWindow;

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化服務
        _settingsService = new SettingsService();
        _screenCaptureService = new ScreenCaptureService(_settingsService);
        _geminiService = new GeminiService(_settingsService);
        _speechService = new TextToSpeechService(_settingsService);
        _speechToTextService = new SpeechToTextService(_settingsService);
        
        // 創建對話歷史視窗（但不顯示）
        _historyWindow = new ConversationHistoryWindow();
        
        // 註冊事件
        _screenCaptureService.ScreensCaptureBatch += OnScreensCaptureBatch;
        _speechToTextService.SpeechRecognized += OnSpeechRecognized;
        _speechToTextService.SilenceDetected += OnSilenceDetected;
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
        // 確保停止語音識別
        if (_isListening)
        {
            StopListening();
        }
        
        // 確保停止截圖
        if (_isCapturing)
        {
            _screenCaptureService.StopCapturing();
        }
        
        // 關閉對話歷史視窗
        _historyWindow.Close();
        
        base.OnClosed(e);
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        SubmitQuery();
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

    private void BtnMicrophone_Click(object sender, RoutedEventArgs e)
    {
        if (_isListening)
        {
            StopListening();
        }
        else
        {
            StartListening();
        }
    }
    
    private async void StartListening()
    {
        try
        {
            // 先中斷正在播放的語音
            _speechService.Stop();
            
            // 清空輸入框
            txtPrompt.Text = string.Empty;
            
            // 變更麥克風按鈕外觀
            iconMicNormal.Visibility = Visibility.Collapsed;
            iconMicListening.Visibility = Visibility.Visible;
            
            try
            {
                // 開始語音識別
                await _speechToTextService.StartListeningAsync();
                _isListening = true;
                
                // 更新狀態
                SetStatus("語音識別中... 請說出您的問題");
            }
            catch (Exception ex)
            {
                // 如果語音識別失敗，恢復按鈕狀態
                iconMicNormal.Visibility = Visibility.Visible;
                iconMicListening.Visibility = Visibility.Collapsed;
                
                // 顯示錯誤訊息 (已經在服務中顯示了詳細錯誤)
                SetStatus("語音識別無法啟動");
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"啟動語音識別失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
            
            // 恢復按鈕狀態
            iconMicNormal.Visibility = Visibility.Visible;
            iconMicListening.Visibility = Visibility.Collapsed;
        }
    }
    
    private async void StopListening()
    {
        if (!_isListening)
            return;
            
        try
        {
            // 停止語音識別
            await _speechToTextService.StopListeningAsync();
            _isListening = false;
            
            // 變更麥克風按鈕外觀
            iconMicNormal.Visibility = Visibility.Visible;
            iconMicListening.Visibility = Visibility.Collapsed;
            
            // 更新狀態
            SetStatus("語音識別已停止");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"停止語音識別失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
        }
    }
    
    private void OnSpeechRecognized(object sender, string recognizedText)
    {
        // 在UI線程上執行
        this.Dispatcher.Invoke(() => 
        {
            // 將識別的文字添加到文字框
            txtPrompt.Text = recognizedText;
        });
    }
    
    private void OnSilenceDetected(object sender, EventArgs e)
    {
        // 在UI線程上執行
        this.Dispatcher.Invoke(() => 
        {
            // 檢測到靜音，自動停止語音識別
            if (_isListening)
            {
                StopListening();
            }
        });
    }

    private void TxtPrompt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 按下 Enter 鍵提交
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            e.Handled = true;  // 防止 Enter 鍵換行
            
            // 相當於點擊提交按鈕
            SubmitQuery();
        }
    }
    
    private void SubmitQuery()
    {
        // 取得輸入文字
        string userPrompt = txtPrompt.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(userPrompt))
            return;
            
        // 中斷任何正在播放的語音
        _speechService.Stop();
        
        // 如果語音識別還在進行中，先停止
        if (_isListening)
        {
            StopListening();
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
        }
        else
        {
            // 如果沒有在截圖，則直接提交文本查詢
            SubmitTextOnlyQuery(userPrompt);
        }
        
        // 清空輸入框
        txtPrompt.Text = string.Empty;
    }

    private async void SubmitTextOnlyQuery(string userPrompt)
    {
        try
        {
            // 顯示處理中的消息
            SetStatus("處理中...");

            // 調用 Gemini API 處理不含截圖的查詢
            await ProcessGeminiRequestAsync(new List<byte[]>(), userPrompt);
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
}