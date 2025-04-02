using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenAgent;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 添加未處理異常處理器
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        
        // 確保可以訪問系統的 Drawing 功能
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        
        // 關鍵配置：啟用 System.Drawing 在 WPF 應用程式中的功能
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // 這個配置允許 System.Drawing 在非 Windows Forms 應用程式中運行
            System.AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
        }
    }
    
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception exception = e.ExceptionObject as Exception;
        System.Windows.MessageBox.Show($"發生未處理的異常: {exception?.Message ?? "未知錯誤"}", 
                                       "錯誤", 
                                       MessageBoxButton.OK, 
                                       MessageBoxImage.Error);
    }
    
    private void App_DispatcherUnhandledException(object sender, 
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show($"發生未處理的UI異常: {e.Exception.Message}", 
                                       "錯誤", 
                                       MessageBoxButton.OK, 
                                       MessageBoxImage.Error);
        e.Handled = true;
    }
}

