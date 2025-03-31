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
    private bool _isCapturing = false;
    private ConversationHistoryWindow _historyWindow;

    public MainWindow()
    {
        InitializeComponent();
        
        // 初始化服務
        _settingsService = new SettingsService();
        _screenCaptureService = new ScreenCaptureService(_settingsService);
        _geminiService = new GeminiService(_settingsService);
        _speechService = new TextToSpeechService(_settingsService);
        
        // 創建對話歷史視窗（但不顯示）
        _historyWindow = new ConversationHistoryWindow();
        
        // 註冊事件
        _screenCaptureService.ScreensCaptureBatch += OnScreensCaptureBatch;
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
            // 使用圖標，所以不再更新按鈕文字
            _isCapturing = true;
            
            // 切換圖標 - 顯示停止圖標，隱藏開始圖標
            iconRecord.Visibility = Visibility.Collapsed;
            iconStop.Visibility = Visibility.Visible;
            
            // 提示用戶截圖已開始
            SetStatus("截圖中... 請輸入問題並按「提交」發送查詢");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"啟動螢幕捕捉失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
        }
    }
    
    private void StopCapturing()
    {
        try
        {
            SubmitScreenCaptures();
            _screenCaptureService.StopCapturing();
            // 使用圖標，所以不再更新按鈕文字
            _isCapturing = false;
            
            // 切換圖標 - 顯示開始圖標，隱藏停止圖標
            iconRecord.Visibility = Visibility.Visible;
            iconStop.Visibility = Visibility.Collapsed;
            
            // 更新狀態
            SetStatus("處理中...");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"停止螢幕捕捉失敗: {ex.Message}", 
                            "錯誤", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
        }
    }
    
    private void SubmitScreenCaptures()
    {
        string userPrompt = txtPrompt.Text.Trim();
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            System.Windows.MessageBox.Show("請輸入問題再提交", "需要輸入", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 中斷任何正在播放的語音
        _speechService.Stop();

        SetStatus("正在處理您的請求...");
        
        if (_isCapturing)
        {
            // 提交當前收集的截圖
            _screenCaptureService.SubmitCapturedBatch();
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

    private async void OnScreensCaptureBatch(object sender, List<byte[]> imageBytesList)
    {            
        try
        {
            string userPrompt = txtPrompt.Text.Trim();
            
            // 顯示處理中的消息
            SetStatus($"處理中... ( {imageBytesList.Count} 張截圖)");
            
            // 處理包含截圖的請求
            await ProcessGeminiRequestAsync(imageBytesList, userPrompt);
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
        // 調用 Gemini API
        string response = await _geminiService.GetResponseFromImageBatchAndTextAsync(imageBytesList, userPrompt);
        response = response.Replace("**","");
        
        _historyWindow.DisplayResponse(userPrompt, response);
        
        // 文字轉語音
        _speechService.Speak(response);
        
        // 更新狀態
        SetStatus("已完成回應");
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
        // 關閉應用程式時需要一併關閉對話歷史視窗
        _historyWindow.Close();
        base.OnClosed(e);
    }

    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        SubmitScreenCaptures();
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
}