using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Modulus.App.Controls.ViewModels;

namespace Modulus.App.Services
{
    /// <summary>
    /// 基于约定的导航服务，自动处理View和ViewModel的映射关系
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _viewTypes = new();
        private readonly Dictionary<string, Type> _viewModelTypes = new();
        private readonly Dictionary<string, object> _cachedViewModels = new();
        
        // 导航历史记录
        private readonly Stack<NavigationHistoryEntry> _backStack = new();
        private readonly Stack<NavigationHistoryEntry> _forwardStack = new();
        
        // 导航生命周期处理器
        private readonly Dictionary<string, Action<object?>> _navigatedToHandlers = new();
        private readonly Dictionary<string, Func<bool>> _navigatedFromHandlers = new();
        
        private NavigationViewModel? _navigationViewModel;
        private string _currentViewName = string.Empty;
        
        /// <summary>
        /// 获取当前活动的页面路由
        /// </summary>
        public string CurrentRoute { get; private set; } = string.Empty;
        
        /// <summary>
        /// 获取是否可以回退
        /// </summary>
        public bool CanGoBack => _backStack.Count > 0;
        
        /// <summary>
        /// 获取是否可以前进
        /// </summary>
        public bool CanGoForward => _forwardStack.Count > 0;
        
        /// <summary>
        /// 构造函数，注入服务提供器
        /// </summary>
        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // 在构造函数中扫描并注册所有View和ViewModel
            RegisterViewsAndViewModels();
        }
        
        /// <summary>
        /// 设置导航视图模型，用于更新导航状态
        /// </summary>
        public void SetNavigationViewModel(NavigationViewModel navigationViewModel)
        {
            _navigationViewModel = navigationViewModel;
        }
        
        /// <summary>
        /// 注册导航到处理器
        /// </summary>
        public void RegisterNavigatedToHandler(string viewName, Action<object?> handler)
        {
            _navigatedToHandlers[viewName] = handler;
        }
        
        /// <summary>
        /// 注册导航离开处理器
        /// </summary>
        public void RegisterNavigatedFromHandler(string viewName, Func<bool> handler)
        {
            _navigatedFromHandlers[viewName] = handler;
        }
        
        /// <summary>
        /// 从当前页面导航离开
        /// </summary>
        public bool NavigateFrom(string viewName)
        {
            if (string.IsNullOrEmpty(viewName) || viewName != _currentViewName)
                return true;
                
            // 检查是否有注册的导航离开处理器
            if (_navigatedFromHandlers.TryGetValue(viewName, out var handler))
            {
                // 调用处理器，如果返回false表示不允许离开
                return handler();
            }
            
            // 默认允许离开
            return true;
        }
        
        /// <summary>
        /// 导航回退
        /// </summary>
        public bool GoBack()
        {
            if (!CanGoBack)
                return false;
                
            // 获取当前页面，用于前进栈
            var currentEntry = new NavigationHistoryEntry
            {
                ViewName = _currentViewName,
                Parameter = null // 当前没有存储参数
            };
            
            // 检查是否可以从当前页面离开
            if (!NavigateFrom(_currentViewName))
                return false;
                
            // 将当前页面添加到前进栈
            _forwardStack.Push(currentEntry);
            
            // 获取回退栈顶的页面
            var backEntry = _backStack.Pop();
            
            // 导航到回退页面
            var result = InternalNavigateTo(backEntry.ViewName, backEntry.Parameter, false);
            
            return result != null;
        }
        
        /// <summary>
        /// 导航前进
        /// </summary>
        public bool GoForward()
        {
            if (!CanGoForward)
                return false;
                
            // 获取当前页面，用于回退栈
            var currentEntry = new NavigationHistoryEntry
            {
                ViewName = _currentViewName,
                Parameter = null // 当前没有存储参数
            };
            
            // 检查是否可以从当前页面离开
            if (!NavigateFrom(_currentViewName))
                return false;
                
            // 将当前页面添加到回退栈
            _backStack.Push(currentEntry);
            
            // 获取前进栈顶的页面
            var forwardEntry = _forwardStack.Pop();
            
            // 导航到前进页面
            var result = InternalNavigateTo(forwardEntry.ViewName, forwardEntry.Parameter, false);
            
            return result != null;
        }
        
        /// <summary>
        /// 导航到指定页面
        /// </summary>
        public Control? NavigateTo(string viewName, object? parameter = null)
        {
            // 检查是否可以从当前页面离开
            if (!NavigateFrom(_currentViewName))
                return null;
                
            // 如果之前有页面，将其添加到回退栈
            if (!string.IsNullOrEmpty(_currentViewName))
            {
                _backStack.Push(new NavigationHistoryEntry
                {
                    ViewName = _currentViewName,
                    Parameter = null // 当前没有存储参数
                });
            }
            
            // 清空前进栈
            _forwardStack.Clear();
            
            // 内部导航实现
            return InternalNavigateTo(viewName, parameter, true);
        }
        
        /// <summary>
        /// 内部导航实现，不处理历史记录
        /// </summary>
        private Control? InternalNavigateTo(string viewName, object? parameter, bool updateActive)
        {
            if (string.IsNullOrEmpty(viewName))
                return null;
                
            // 更新当前视图名称
            _currentViewName = viewName;
            CurrentRoute = viewName;
            
            // 激活导航项
            if (updateActive)
            {
                _navigationViewModel?.SetActiveNavigationItem(viewName);
            }
            
            // 根据约定查找View类型
            if (!_viewTypes.TryGetValue(viewName, out var viewType) && !_viewTypes.Values.Any(t => t.Name == viewName))
            {
                Console.WriteLine($"未找到视图 {viewName} 对应的View类型");
                return null;
            }
            
            // 使用找到的类型或通过名称找到的类型
            viewType ??= _viewTypes.Values.First(t => t.Name == viewName);
            
            // 获取对应的ViewModel类型
            var viewModelType = GetViewModelTypeForView(viewType.Name);
            if (viewModelType == null)
            {
                Console.WriteLine($"未找到View {viewType.Name} 对应的ViewModel类型");
                // 即使没有找到ViewModel，我们仍然可以创建并返回View
            }
            
            try
            {
                // 获取或创建ViewModel实例
                object? viewModel = null;
                
                if (viewModelType != null)
                {
                    // 检查是否已缓存该ViewModel（单例模式）
                    if (!_cachedViewModels.TryGetValue(viewModelType.FullName!, out viewModel))
                    {
                        // 尝试从DI容器获取实例
                        viewModel = _serviceProvider.GetService(viewModelType);
                        
                        // 如果DI容器没有提供，则手动创建实例
                        if (viewModel == null)
                        {
                            viewModel = Activator.CreateInstance(viewModelType);
                        }
                        
                        // 缓存实例
                        if (viewModel != null)
                        {
                            _cachedViewModels[viewModelType.FullName!] = viewModel;
                        }
                    }
                }
                
                // 创建View实例
                Control? view = null;
                
                // 检查是否有接受ViewModel的构造函数
                var ctor = viewType.GetConstructors()
                    .FirstOrDefault(c => c.GetParameters().Length == 1 && 
                                       c.GetParameters()[0].ParameterType == viewModelType);
                
                if (ctor != null && viewModel != null)
                {
                    // 使用接收ViewModel的构造函数创建View
                    view = (Control)ctor.Invoke(new[] { viewModel });
                }
                else
                {
                    // 使用默认构造函数创建View
                    view = (Control)Activator.CreateInstance(viewType)!;
                    
                    // 如果有ViewModel，手动设置DataContext
                    if (viewModel != null)
                    {
                        view.DataContext = viewModel;
                    }
                }
                
                // 更新NavigationViewModel的CurrentPage和CurrentPageTitle
                if (_navigationViewModel != null && view != null)
                {
                    // 查找对应的NavigationItemModel获取标题
                    var navigationItem = _navigationViewModel.GetNavigationItem(viewName);
                    
                    _navigationViewModel.CurrentPage = view;
                    _navigationViewModel.CurrentPageTitle = navigationItem?.Label ?? viewName;
                    
                    // 如果是在小屏幕上且导航栏展开，自动折叠导航栏
                    if (_navigationViewModel.IsNavigationOverlayed && _navigationViewModel.IsNavigationExpanded)
                    {
                        _navigationViewModel.ToggleNavigationBarCommand.Execute(null);
                    }
                }
                
                // 触发导航到事件
                if (_navigatedToHandlers.TryGetValue(viewName, out var handler))
                {
                    handler(parameter);
                }
                
                return view;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导航到 {viewName} 时发生错误: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 注册导航项目
        /// </summary>
        public void RegisterNavigationItem(string label, string icon, string route, string section = "body")
        {
            if (_navigationViewModel == null)
                return;
                
            _navigationViewModel.AddNavigationItem(label, icon, route, section);
        }
        
        /// <summary>
        /// 扫描程序集中所有View和ViewModel并建立映射关系
        /// </summary>
        private void RegisterViewsAndViewModels()
        {
            // 获取当前程序集
            var assembly = Assembly.GetExecutingAssembly();
            
            // 查找所有View（以View结尾的类）
            var viewTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && 
                           (t.Name.EndsWith("View") || t.Name.EndsWith("Page")) &&
                           typeof(Control).IsAssignableFrom(t));
            
            // 查找所有ViewModel（以ViewModel结尾的类）
            var viewModelTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && 
                           t.Name.EndsWith("ViewModel") &&
                           typeof(ObservableObject).IsAssignableFrom(t));
            
            // 注册所有View
            foreach (var viewType in viewTypes)
            {
                _viewTypes[viewType.Name] = viewType;
                Console.WriteLine($"注册View: {viewType.Name}");
            }
            
            // 注册所有ViewModel
            foreach (var viewModelType in viewModelTypes)
            {
                _viewModelTypes[viewModelType.Name] = viewModelType;
                Console.WriteLine($"注册ViewModel: {viewModelType.Name}");
            }
        }
        
        /// <summary>
        /// 根据View类名获取对应的ViewModel类型
        /// </summary>
        private Type? GetViewModelTypeForView(string viewName)
        {
            // 命名约定：如果View名称为XxxxView或XxxxPage，对应的ViewModel名称应为XxxxViewModel
            string baseName = viewName;
            
            if (viewName.EndsWith("View"))
            {
                baseName = viewName.Substring(0, viewName.Length - 4);
            }
            else if (viewName.EndsWith("Page"))
            {
                baseName = viewName.Substring(0, viewName.Length - 4);
            }
            
            string viewModelName = $"{baseName}ViewModel";
            
            // 查找匹配的ViewModel
            if (_viewModelTypes.TryGetValue(viewModelName, out var viewModelType))
            {
                return viewModelType;
            }
            
            return null;
        }
        
        /// <summary>
        /// Registers a view model factory for plugin navigation.
        /// </summary>
        /// <param name="viewName">The unique view name.</param>
        /// <param name="factory">A factory function that returns a new ViewModel instance.</param>
        public void RegisterViewModel(string viewName, Func<object> factory)
        {
            // Store the factory in a dictionary for plugin navigation
            if (string.IsNullOrWhiteSpace(viewName) || factory == null)
                return;
            // Use a separate dictionary for plugin factories
            if (_pluginViewModelFactories == null)
                _pluginViewModelFactories = new Dictionary<string, Func<object>>();
            _pluginViewModelFactories[viewName] = factory;
        }
        private Dictionary<string, Func<object>>? _pluginViewModelFactories;
    }
    
    /// <summary>
    /// 导航历史记录条目
    /// </summary>
    internal class NavigationHistoryEntry
    {
        /// <summary>
        /// 视图名称
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        
        /// <summary>
        /// 导航参数
        /// </summary>
        public object? Parameter { get; set; }
    }
}