using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Services;

namespace Modulus.App.ViewModels
{
    /// <summary>
    /// 仪表盘视图模型
    /// </summary>
    public partial class DashboardViewModel : NavigationViewModelBase
    {
        /// <summary>
        /// 视图名称
        /// </summary>
        public override string ViewName => "DashboardView";
        
        [ObservableProperty]
        private string title = "仪表盘";
        
        [ObservableProperty]
        private string welcomeMessage = "欢迎使用Modulus应用程序";
        
        [ObservableProperty]
        private string lastUpdated = "最后更新: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        /// <summary>
        /// 创建仪表盘视图模型
        /// </summary>
        public DashboardViewModel(INavigationService navigationService) : base(navigationService)
        {
        }
        
        /// <summary>
        /// 当导航到此页面时触发
        /// </summary>
        public override void OnNavigatedTo(object? parameter)
        {
            // 刷新数据
            Refresh();
        }
        
        /// <summary>
        /// 刷新命令
        /// </summary>
        [RelayCommand]
        private void Refresh()
        {
            // 更新最后更新时间
            LastUpdated = "最后更新: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
} 