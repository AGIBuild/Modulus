using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using Modulus.App.Controls.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace Modulus.App.Controls
{
    /// <summary>
    /// 导航主体控件，用于显示主要导航菜单项
    /// </summary>
    public partial class NavigationBody : UserControl
    {
        #region 依赖属性

        /// <summary>
        /// 定义 IsExpanded 依赖属性
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<NavigationBody, bool>(nameof(IsExpanded), defaultValue: false);

        /// <summary>
        /// 获取或设置导航栏是否处于展开状态
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        // Removed NavigationItemsProperty and NavigationItems property

        #endregion

        /// <summary>
        /// 用于直接访问导航命令的属性
        /// </summary>
        public IRelayCommand<NavigationItemModel>? NavigateToViewCommand => 
            (DataContext as NavigationViewModel)?.NavigateToViewCommand;
        
        // 自定义CommandProperty - 使用标准属性而非DirectProperty简化处理
        public static readonly StyledProperty<IRelayCommand<NavigationItemModel>?> NavigateToViewCommandProperty =
            AvaloniaProperty.Register<NavigationBody, IRelayCommand<NavigationItemModel>?>(
                nameof(NavigateToViewCommand));

        public NavigationBody()
        {
            InitializeComponent();
            
            // 监听IsExpanded属性变更
            PropertyChanged += OnPropertyChanged;
            
            // 监听DataContext变更，触发UI更新
            DataContextChanged += OnDataContextChanged;
            
            // 初始化时设置正确的类
            UpdateExpandedClass();
        }
        
        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is NavigationViewModel viewModel)
            {
                // 更新导航命令
                this.SetValue(NavigateToViewCommandProperty, viewModel.NavigateToViewCommand);
            }
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == IsExpandedProperty)
            {
                UpdateExpandedClass();
            }
        }
        
        private void UpdateExpandedClass()
        {
            if (IsExpanded)
            {
                this.Classes.Add("expanded");
            }
            else
            {
                this.Classes.Remove("expanded");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 
