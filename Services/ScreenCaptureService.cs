using System.Drawing.Imaging;
using System.IO;
using System.Timers;

namespace ScreenAgent.Services
{
    public class ScreenCaptureService
    {
        private System.Timers.Timer? _captureTimer;
        private readonly SettingsService _settingsService;
        private List<byte[]> _capturedImages = new List<byte[]>();
        
        // 事件修改為傳送截圖集合而非單一截圖
        public event EventHandler<List<byte[]>>? ScreensCaptureBatch;

        public ScreenCaptureService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // 單次截圖並返回圖片數據
        public byte[] CaptureScreenSingle()
        {
            try
            {
                // 取得主螢幕的尺寸
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
                using (var graphics = Graphics.FromImage(bitmap))
                using (var stream = new MemoryStream())
                {
                    // 設置高品質截圖
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
                    
                    // 將圖像壓縮為JPEG以減少大小，但保持較高品質
                    var encoder = ImageCodecInfo.GetImageEncoders()
                        .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                        
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                    
                    bitmap.Save(stream, encoder, encoderParams);
                    
                    // 返回圖片數據
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                throw new Exception($"無法執行螢幕截圖: {ex.Message}", ex);
            }
        }
        
        // 捕獲螢幕並添加到集合
        private void CaptureScreen()
        {
            try
            {
                // 使用共用方法獲取截圖
                byte[] screenImage = CaptureScreenSingle();
                
                // 將截圖添加到集合中
                _capturedImages.Add(screenImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                // 不要在這裡拋出異常，因為這是在定時器的回調中，可能會導致應用崩潰
            }
        }

        public void StartCapturing()
        {
            try
            {
                // 測試螢幕截圖功能是否正常工作
                TestScreenCapture();
                
                // 清空截圖集合
                _capturedImages.Clear();
                
                // 立即執行一次截圖
                CaptureScreen();
                
                // 若成功，則設置定時器
                int frequency = _settingsService.GetCaptureFrequencyInMilliseconds();
                _captureTimer = new System.Timers.Timer(frequency);
                _captureTimer.Elapsed += OnTimerElapsed;
                _captureTimer.AutoReset = true;
                _captureTimer.Start();
            }
            catch (Exception ex)
            {
                throw new Exception($"無法啟動螢幕捕捉: {ex.Message}", ex);
            }
        }

        private void TestScreenCapture()
        {
            try
            {
                // 使用較小的尺寸進行測試以提高效能
                using (var bitmap = new Bitmap(10, 10))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(10, 10));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"螢幕截圖測試失敗。請確保應用程式有足夠權限: {ex.Message}", ex);
            }
        }

        public void StopCapturing()
        {
            if (_captureTimer != null)
            {
                _captureTimer.Stop();
                _captureTimer.Elapsed -= OnTimerElapsed;
                _captureTimer.Dispose();
                _captureTimer = null;
            }
            
            // 停止時清空截圖集合
            _capturedImages.Clear();
        }

        // 提交當前收集的截圖批次
        public void SubmitCapturedBatch()
        {
            if (_capturedImages.Count == 0)
                return;
                
            // 創建截圖集合的副本並傳送事件
            var imagesToSubmit = new List<byte[]>(_capturedImages);
            
            // 觸發事件，傳送截圖集合
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                ScreensCaptureBatch?.Invoke(this, imagesToSubmit);
            });
            
            // 清空截圖集合，準備收集下一批
            _capturedImages.Clear();
        }
        
        // 明確清空截圖集合的方法
        public void ClearCapturedImages()
        {
            // 清空截圖集合
            _capturedImages.Clear();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            // 在非UI線程上執行截圖，避免凍結UI
            ThreadPool.QueueUserWorkItem(_ => 
            {
                CaptureScreen();
            });
        }
    }
}