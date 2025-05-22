using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Controls;
using Modulus.App.Controls.ViewModels;
using Modulus.App.ViewModels;
using System;

namespace Modulus.App.Views
{
    public partial class NavigationViewDemo : UserControl
    {
        private NavigationViewModel _viewModel;

        public NavigationViewDemo()
        {
            InitializeComponent();
            
            _viewModel = new NavigationViewModel();
            
            // 设置示例页面
            SetupSamplePages();
            
            // 分配ViewModel到NavigationView
            var navigationView = this.FindControl<NavigationView>("MainNavigation");
            if (navigationView != null)
            {
                navigationView.DataContext = _viewModel;
            }
            
            // 注册布局变化事件
            this.AttachedToVisualTree += (s, e) => UpdateViewModelLayout();
            PropertyChanged += (s, e) => {
                if (e.Property == BoundsProperty)
                {
                    UpdateViewModelLayout();
                }
            };
        }

        private void UpdateViewModelLayout()
        {
            var bounds = this.Bounds;
            _viewModel.UpdateLayout(bounds.Width);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private void SetupSamplePages()
        {
            // 创建示例页面
            var dashboardViewModel = new DashboardPlaceholderViewModel();
            var dashboardPage = new DashboardView { DataContext = dashboardViewModel };
            
            var settingsPage = new Border { 
                Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Child = new TextBlock { 
                    Text = "设置页面",
                    FontSize = 24,
                    FontWeight = FontWeight.Bold
                }
            };
            
            var profilePage = new Border { 
                Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Child = new TextBlock { 
                    Text = "个人资料页面",
                    FontSize = 24,
                    FontWeight = FontWeight.Bold
                }
            };
            
            var notificationsPage = new Border { 
                Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Child = new TextBlock { 
                    Text = "通知页面",
                    FontSize = 24,
                    FontWeight = FontWeight.Bold
                }
            };
            
            // 设置初始页面
            _viewModel.CurrentPage = dashboardPage;
            _viewModel.CurrentPageTitle = "仪表盘";
            
            // 添加导航项
            var dashboardItem = _viewModel.AddNavigationItem("仪表盘", "\uE80F", "dashboard");
            dashboardItem.IsActive = true;
            
            var notificationsItem = _viewModel.AddNavigationItem("通知", "\uE7E7", "notifications");
            notificationsItem.SetBadge(3);
            
            var settingsItem = _viewModel.AddNavigationItem("设置", "\uE713", "settings", "footer");
            settingsItem.SetDotBadge();
            
            var profileItem = _viewModel.AddNavigationItem("个人资料", "\uE77B", "profile", "footer");
            
            // 订阅导航命令
            _viewModel.NavigateToViewCommand = new RelayCommand<NavigationItemModel>(item =>
            {
                if (item?.ViewName == "dashboard")
                {
                    _viewModel.CurrentPage = dashboardPage;
                    _viewModel.CurrentPageTitle = "仪表盘";
                }
                else if (item?.ViewName == "notifications")
                {
                    _viewModel.CurrentPage = notificationsPage;
                    _viewModel.CurrentPageTitle = "通知";
                }
                else if (item?.ViewName == "settings")
                {
                    _viewModel.CurrentPage = settingsPage;
                    _viewModel.CurrentPageTitle = "设置";
                }
                else if (item?.ViewName == "profile")
                {
                    _viewModel.CurrentPage = profilePage;
                    _viewModel.CurrentPageTitle = "个人资料";
                }
                
                // 更新活动状态
                if (item != null)
                {
                    _viewModel.SetActiveNavigationItem(item.ViewName);
                }
            });
        }
    }
} 
