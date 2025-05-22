using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Modulus.App.Controls
{
    /// <summary>
    /// 导航头部控件，用于显示应用标识和版本信息
    /// </summary>
    public partial class NavigationHeader : UserControl
    {
        #region 依赖属性

        /// <summary>
        /// 定义 IsExpanded 依赖属性
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<NavigationHeader, bool>(nameof(IsExpanded), defaultValue: false);

        /// <summary>
        /// 获取或设置导航栏是否处于展开状态
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// 定义 AppIcon 依赖属性
        /// </summary>
        public static readonly StyledProperty<string> AppIconProperty =
            AvaloniaProperty.Register<NavigationHeader, string>(
                nameof(AppIcon), defaultValue: "avares://Modulus.App/Assets/avalonia-logo.ico");

        /// <summary>
        /// 获取或设置应用图标路径
        /// </summary>
        public string AppIcon
        {
            get => GetValue(AppIconProperty);
            set => SetValue(AppIconProperty, value);
        }

        /// <summary>
        /// 定义 AppName 依赖属性
        /// </summary>
        public static readonly StyledProperty<string> AppNameProperty =
            AvaloniaProperty.Register<NavigationHeader, string>(nameof(AppName), defaultValue: "Modulus");

        /// <summary>
        /// 获取或设置应用名称
        /// </summary>
        public string AppName
        {
            get => GetValue(AppNameProperty);
            set => SetValue(AppNameProperty, value);
        }

        /// <summary>
        /// 定义 AppVersion 依赖属性
        /// </summary>
        public static readonly StyledProperty<string> AppVersionProperty =
            AvaloniaProperty.Register<NavigationHeader, string>(nameof(AppVersion), defaultValue: "v1.0.0");

        /// <summary>
        /// 获取或设置应用版本
        /// </summary>
        public string AppVersion
        {
            get => GetValue(AppVersionProperty);
            set => SetValue(AppVersionProperty, value);
        }

        #endregion

        public NavigationHeader()
        {
            InitializeComponent();
            
            // 监听IsExpanded属性变更
            PropertyChanged += OnPropertyChanged;
            
            // 初始化时设置正确的类
            UpdateExpandedClass();
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