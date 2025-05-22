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
    /// 导航页脚控件，用于显示底部导航菜单项如设置、帮助等
    /// </summary>
    public partial class NavigationFooter : UserControl
    {
        #region 依赖属性

        /// <summary>
        /// 定义 IsExpanded 依赖属性
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<NavigationFooter, bool>(nameof(IsExpanded), defaultValue: false);

        /// <summary>
        /// 获取或设置导航栏是否处于展开状态
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// 定义 FooterItems 依赖属性
        /// </summary>
        public static readonly StyledProperty<ObservableCollection<NavigationItemModel>> FooterItemsProperty =
            AvaloniaProperty.Register<NavigationFooter, ObservableCollection<NavigationItemModel>>(
                nameof(FooterItems), new ObservableCollection<NavigationItemModel>());

        /// <summary>
        /// 获取或设置页脚导航项列表
        /// </summary>
        public ObservableCollection<NavigationItemModel> FooterItems
        {
            get => GetValue(FooterItemsProperty);
            set => SetValue(FooterItemsProperty, value);
        }

        #endregion
        
        /// <summary>
        /// 用于直接访问导航命令的属性
        /// </summary>
        public IRelayCommand<NavigationItemModel>? NavigateToViewCommand => 
            (DataContext as NavigationViewModel)?.NavigateToViewCommand;
        
        // 自定义CommandProperty - 使用标准属性而非DirectProperty简化处理
        public static readonly StyledProperty<IRelayCommand<NavigationItemModel>?> NavigateToViewCommandProperty =
            AvaloniaProperty.Register<NavigationFooter, IRelayCommand<NavigationItemModel>?>(
                nameof(NavigateToViewCommand));

        public NavigationFooter()
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
                
                // 更新底部导航项列表 - 重要：确保菜单项数据正确传递
                if (viewModel.FooterItems != null)
                {
                    this.FooterItems = viewModel.FooterItems;
                }
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