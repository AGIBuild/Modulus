using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Modulus.App.Controls.ViewModels;

namespace Modulus.App.Controls
{
    /// <summary>
    /// 现代化导航视图组件，提供页面导航和内容区域的布局
    /// </summary>
    public partial class NavigationView : UserControl
    {
        #region 依赖属性
        
        /// <summary>
        /// 定义 ShowHeader 依赖属性
        /// </summary>
        public static readonly StyledProperty<bool> ShowHeaderProperty =
            AvaloniaProperty.Register<NavigationView, bool>(nameof(ShowHeader), defaultValue: true);

        /// <summary>
        /// 获取或设置是否显示头部区域
        /// </summary>
        public bool ShowHeader
        {
            get => GetValue(ShowHeaderProperty);
            set => SetValue(ShowHeaderProperty, value);
        }
        
        /// <summary>
        /// 定义 ShowBody 依赖属性
        /// </summary>
        public static readonly StyledProperty<bool> ShowBodyProperty =
            AvaloniaProperty.Register<NavigationView, bool>(nameof(ShowBody), defaultValue: true);

        /// <summary>
        /// 获取或设置是否显示主体区域
        /// </summary>
        public bool ShowBody
        {
            get => GetValue(ShowBodyProperty);
            set => SetValue(ShowBodyProperty, value);
        }
        
        /// <summary>
        /// 定义 ShowFooter 依赖属性
        /// </summary>
        public static readonly StyledProperty<bool> ShowFooterProperty =
            AvaloniaProperty.Register<NavigationView, bool>(nameof(ShowFooter), defaultValue: true);

        /// <summary>
        /// 获取或设置是否显示底部区域
        /// </summary>
        public bool ShowFooter
        {
            get => GetValue(ShowFooterProperty);
            set => SetValue(ShowFooterProperty, value);
        }
        
        #endregion

        private NavigationHeader? _navigationHeader;
        private NavigationBody? _navigationBody;
        private NavigationFooter? _navigationFooter;

        public NavigationView()
        {
            InitializeComponent();
            
            // 获取子组件引用
            _navigationHeader = this.FindControl<NavigationHeader>("NavHeader");
            _navigationBody = this.FindControl<NavigationBody>("NavBody");
            _navigationFooter = this.FindControl<NavigationFooter>("NavFooter");
            
            // 在加载后检查并绑定ViewModel
            this.AttachedToVisualTree += OnAttachedToVisualTree;
            
            // 监听DataContext变化以传递给子控件
            this.DataContextChanged += NavigationView_DataContextChanged;
        }
        
        private void NavigationView_DataContextChanged(object? sender, System.EventArgs e)
        {
            PropagateDataContextToChildren();
        }
        
        private void PropagateDataContextToChildren()
        {
            if (DataContext is NavigationViewModel viewModel)
            {
                // 传递DataContext给子控件，确保它们可以访问命令和属性
                if (_navigationBody != null)
                {
                    _navigationBody.DataContext = viewModel;
                }
                
                if (_navigationFooter != null)
                {
                    _navigationFooter.DataContext = viewModel;
                }
                
                if (_navigationHeader != null)
                {
                    _navigationHeader.DataContext = viewModel;
                }
            }
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            EnsureViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// 确保ViewModel存在，如果不存在则创建
        /// </summary>
        private void EnsureViewModel()
        {
            if (DataContext is NavigationViewModel vm)
            {
                // 从控件绑定属性
                vm.ShowHeader = ShowHeader;
                vm.ShowBody = ShowBody;
                vm.ShowFooter = ShowFooter;
                
                // 监听NavigationViewModel的IsNavigationExpanded变化
                vm.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(NavigationViewModel.IsNavigationExpanded))
                    {
                        // 确保子组件立即更新展开状态
                        ForceUpdateChildrenExpandedState(vm.IsNavigationExpanded);
                    }
                };
                
                // 初始化时更新一次
                ForceUpdateChildrenExpandedState(vm.IsNavigationExpanded);
                
                // 确保子控件获得DataContext
                PropagateDataContextToChildren();
            }
        }
        
        /// <summary>
        /// 强制更新所有子组件的展开状态
        /// </summary>
        private void ForceUpdateChildrenExpandedState(bool isExpanded)
        {
            // 查找子组件（如果未找到则尝试通过XAML查找）
            _navigationBody ??= this.FindControl<NavigationBody>("NavBody");
            _navigationFooter ??= this.FindControl<NavigationFooter>("NavFooter");
            
            // 更新组件状态
            if (_navigationBody != null)
            {
                _navigationBody.SetValue(NavigationBody.IsExpandedProperty, isExpanded);
            }
            
            if (_navigationFooter != null)
            {
                _navigationFooter.SetValue(NavigationFooter.IsExpandedProperty, isExpanded);
            }
        }

        /// <summary>
        /// 处理导航遮罩点击事件，展开或折叠导航栏
        /// </summary>
        private void NavOverlay_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (DataContext is NavigationViewModel vm && vm.IsNavigationExpanded)
            {
                vm.ToggleNavigationBarCommand.Execute(null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 程序化折叠导航栏
        /// </summary>
        public void CollapseNavigationBar()
        {
            if (DataContext is NavigationViewModel vm && vm.IsNavigationExpanded)
            {
                vm.ToggleNavigationBarCommand.Execute(null);
            }
        }

        /// <summary>
        /// 程序化展开导航栏
        /// </summary>
        public void ExpandNavigationBar()
        {
            if (DataContext is NavigationViewModel vm && !vm.IsNavigationExpanded)
            {
                vm.ToggleNavigationBarCommand.Execute(null);
            }
        }
    }
} 