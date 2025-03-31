using System;
using System.Windows;
using System.Windows.Input;
using ScreenAgent.Models;

namespace ScreenAgent.Views
{
    public partial class ConversationHistoryWindow : Window
    {
        public ConversationHistoryWindow()
        {
            InitializeComponent();
            
            // 設置視窗位置在螢幕右方 1/4 區域
            PositionWindowAtRightSide();
            
            // 添加拖曳事件處理
            headerPanel.MouseLeftButtonDown += HeaderPanel_MouseLeftButtonDown;
        }
        
        private void HeaderPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 當用戶按下滑鼠左鍵時，開始拖曳視窗
            this.DragMove();
        }
        
        public void PositionWindowAtRightSide()
        {
            // 獲取螢幕尺寸
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // 設置視窗寬度為螢幕的 1/4，高度為螢幕的 1/2
            this.Width = screenWidth / 4;
            this.Height = screenHeight / 2; // 改為螢幕高度的一半
            
            // 位置在螢幕右側
            this.Left = screenWidth - this.Width - 20; // 右邊留20像素邊距
            this.Top = 20; // 頂部留20像素邊距
        }
        
        // 顯示單一問答響應
        public void DisplayResponse(string userPrompt, string aiResponse)
        {
            Dispatcher.Invoke(() =>
            {
                userPromptText.Text = userPrompt;
                aiResponseText.Text = aiResponse;
                timestampText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            });
        }
    }
}