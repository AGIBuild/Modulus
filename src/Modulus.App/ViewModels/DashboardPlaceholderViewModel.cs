using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Modulus.App.ViewModels
{
    /// <summary>
    /// 仪表盘占位视图模型
    /// </summary>
    public partial class DashboardPlaceholderViewModel : ObservableObject
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = "仪表盘";
        
        /// <summary>
        /// 欢迎信息
        /// </summary>
        public string WelcomeMessage { get; set; } = "欢迎使用Modulus应用程序";

        /// <summary>
        /// Gets or sets the summary text.
        /// </summary>
        [ObservableProperty]
        private string summaryText = "这是一个基于插件的应用程序框架，您可以通过添加插件来扩展功能。";
        
        /// <summary>
        /// Gets or sets the list of recent activities.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ActivityItem> recentActivities = new();
        
        /// <summary>
        /// Gets or sets the list of statistics.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<StatItem> statistics = new();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardPlaceholderViewModel"/> class.
        /// </summary>
        public DashboardPlaceholderViewModel()
        {
            InitializeSampleData();
        }
        
        private void InitializeSampleData()
        {
            // Add sample activities
            RecentActivities.Add(new ActivityItem { Title = "安装了新插件", Description = "Plugin A v1.2.0", Time = "今天 10:30" });
            RecentActivities.Add(new ActivityItem { Title = "更新了系统设置", Description = "更改了主题和语言设置", Time = "昨天 15:45" });
            RecentActivities.Add(new ActivityItem { Title = "备份了配置", Description = "自动备份到本地存储", Time = "2天前" });
            
            // Add sample statistics
            Statistics.Add(new StatItem { Label = "已安装插件", Value = "5" });
            Statistics.Add(new StatItem { Label = "活跃插件", Value = "3" });
            Statistics.Add(new StatItem { Label = "系统版本", Value = "1.0.0" });
            Statistics.Add(new StatItem { Label = "上次更新", Value = "2天前" });
        }
    }
    
    /// <summary>
    /// Represents an activity item for display in the recent activities list.
    /// </summary>
    public class ActivityItem
    {
        /// <summary>
        /// Gets or sets the activity title.
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the activity description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the activity time.
        /// </summary>
        public string Time { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents a statistic item for display in the statistics panel.
    /// </summary>
    public class StatItem
    {
        /// <summary>
        /// Gets or sets the statistic label.
        /// </summary>
        public string Label { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the statistic value.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
} 