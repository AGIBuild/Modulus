using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Windows.Input;

namespace Modulus.App.Controls
{
    /// <summary>
    /// 内容区域控件，用于显示当前选中的页面
    /// </summary>
    public partial class ContentArea : UserControl
    {
        #region 依赖属性

        /// <summary>
        /// 定义 ShowToggleButton 依赖属性
        /// </summary>
        public static readonly StyledProperty<bool> ShowToggleButtonProperty =
            AvaloniaProperty.Register<ContentArea, bool>(nameof(ShowToggleButton), defaultValue: true);

        /// <summary>
        /// 获取或设置是否显示折叠/展开按钮
        /// </summary>
        public bool ShowToggleButton
        {
            get => GetValue(ShowToggleButtonProperty);
            set => SetValue(ShowToggleButtonProperty, value);
        }

        /// <summary>
        /// 定义 PageTitle 依赖属性
        /// </summary>
        public static readonly StyledProperty<string> PageTitleProperty =
            AvaloniaProperty.Register<ContentArea, string>(nameof(PageTitle), defaultValue: "页面标题");

        /// <summary>
        /// 获取或设置页面标题
        /// </summary>
        public string PageTitle
        {
            get => GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        /// <summary>
        /// 定义 CollapseExpandIcon 依赖属性
        /// </summary>
        public static readonly StyledProperty<string> CollapseExpandIconProperty =
            AvaloniaProperty.Register<ContentArea, string>(nameof(CollapseExpandIcon), defaultValue: "menu_regular");

        /// <summary>
        /// 获取或设置折叠/展开按钮图标
        /// </summary>
        public string CollapseExpandIcon
        {
            get => GetValue(CollapseExpandIconProperty);
            set => SetValue(CollapseExpandIconProperty, value);
        }

        /// <summary>
        /// 定义 ContentValue 依赖属性
        /// </summary>
        public static readonly StyledProperty<object> ContentValueProperty =
            AvaloniaProperty.Register<ContentArea, object>(nameof(ContentValue));

        /// <summary>
        /// 获取或设置内容
        /// </summary>
        public object ContentValue
        {
            get => GetValue(ContentValueProperty);
            set => SetValue(ContentValueProperty, value);
        }

        /// <summary>
        /// 定义 ContentMargin 依赖属性
        /// </summary>
        public static readonly StyledProperty<Thickness> ContentMarginProperty =
            AvaloniaProperty.Register<ContentArea, Thickness>(nameof(ContentMargin), new Thickness(24));

        /// <summary>
        /// 获取或设置内容边距
        /// </summary>
        public Thickness ContentMargin
        {
            get => GetValue(ContentMarginProperty);
            set => SetValue(ContentMarginProperty, value);
        }

        /// <summary>
        /// 定义 Background 依赖属性
        /// </summary>
        public static readonly new StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<ContentArea, IBrush>(nameof(Background), 
                new SolidColorBrush(Color.Parse("#F9FAFB")));

        /// <summary>
        /// 获取或设置背景色
        /// </summary>
        public new IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// 定义 ToggleCommand 依赖属性
        /// </summary>
        public static readonly StyledProperty<ICommand> ToggleCommandProperty =
            AvaloniaProperty.Register<ContentArea, ICommand>(nameof(ToggleCommand));

        /// <summary>
        /// 获取或设置折叠/展开命令
        /// </summary>
        public ICommand ToggleCommand
        {
            get => GetValue(ToggleCommandProperty);
            set => SetValue(ToggleCommandProperty, value);
        }

        #endregion

        public ContentArea()
        {
            InitializeComponent();
            
            // 注册折叠/展开按钮点击事件
            var toggleButton = this.FindControl<Button>("ToggleButton");
            if (toggleButton != null)
            {
                toggleButton.Click += (s, e) =>
                {
                    if (ToggleCommand?.CanExecute(null) == true)
                    {
                        ToggleCommand.Execute(null);
                    }
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 