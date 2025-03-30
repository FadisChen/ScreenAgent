using System;
using System.Collections.ObjectModel;
using System.Windows;
using ScreenAgent.Models;

namespace ScreenAgent.Views
{
    public partial class ConversationHistoryWindow : Window
    {
        private ObservableCollection<ConversationItem> _conversations;

        public ConversationHistoryWindow()
        {
            InitializeComponent();
            _conversations = new ObservableCollection<ConversationItem>();
            conversationHistory.ItemsSource = _conversations;
            
            // 設置視窗位置在螢幕右方 1/4 區域
            PositionWindowAtRightSide();
        }
        
        public void PositionWindowAtRightSide()
        {
            // 獲取螢幕尺寸
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            // 設置視窗寬度為螢幕的 1/4
            this.Width = screenWidth / 4;
            this.Height = screenHeight - 100; // 給任務欄和頂部留點空間
            
            // 位置在螢幕右側
            this.Left = screenWidth - this.Width - 20; // 右邊留20像素邊距
            this.Top = 20; // 頂部留20像素邊距
        }
        
        public void AddConversationItem(ConversationItem item)
        {
            Dispatcher.Invoke(() =>
            {
                _conversations.Insert(0, item); // 新對話放在最上面
            });
        }
        
        public void ClearConversations()
        {
            Dispatcher.Invoke(() =>
            {
                _conversations.Clear();
            });
        }
        
        // 提供 ClearHistory 作為 ClearConversations 的別名，以保持兼容性
        public void ClearHistory()
        {
            ClearConversations();
        }
        
        // 顯示單一問答響應，不保留歷史記錄
        public void DisplayResponse(string userPrompt, string aiResponse)
        {
            Dispatcher.Invoke(() =>
            {
                _conversations.Clear();
                
                AddConversationItem(new ConversationItem
                {
                    UserPrompt = userPrompt,
                    AiResponse = aiResponse,
                    Timestamp = DateTime.Now
                });
                
                // 滾動到頂部以確保用戶可以看到最新的回應
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToTop();
                }
            });
        }
    }
}