using Avalonia.Controls;
using Modulus.App.Controls.ViewModels;
using System;

namespace Modulus.App.Services
{
    /// <summary>
    /// 导航服务接口
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 获取当前活动的页面路由
        /// </summary>
        string CurrentRoute { get; }
        
        /// <summary>
        /// 设置导航视图模型，用于更新导航状态
        /// </summary>
        void SetNavigationViewModel(NavigationViewModel navigationViewModel);
        
        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameter">导航参数</param>
        Control? NavigateTo(string viewName, object? parameter = null);
        
        /// <summary>
        /// 从当前页面导航离开
        /// </summary>
        /// <param name="viewName">当前视图名称</param>
        /// <returns>如果可以离开返回true，否则返回false</returns>
        bool NavigateFrom(string viewName);
        
        /// <summary>
        /// 导航回退
        /// </summary>
        /// <returns>如果回退成功返回true，否则返回false</returns>
        bool GoBack();
        
        /// <summary>
        /// 导航前进
        /// </summary>
        /// <returns>如果前进成功返回true，否则返回false</returns>
        bool GoForward();
        
        /// <summary>
        /// 注册导航进入处理器
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="handler">进入处理器</param>
        void RegisterNavigatedToHandler(string viewName, Action<object?> handler);
        
        /// <summary>
        /// 注册导航离开处理器
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="handler">离开处理器</param>
        void RegisterNavigatedFromHandler(string viewName, Func<bool> handler);
        
        /// <summary>
        /// 获取可以回退的状态
        /// </summary>
        bool CanGoBack { get; }
        
        /// <summary>
        /// 获取可以前进的状态
        /// </summary>
        bool CanGoForward { get; }
        
        /// <summary>
        /// 注册导航项目
        /// </summary>
        /// <param name="label">显示的标签文本</param>
        /// <param name="icon">图标（使用Segoe MDL2 Assets字体）</param>
        /// <param name="route">路由名称（通常是View类名）</param>
        /// <param name="section">导航项所在区域（body或footer）</param>
        void RegisterNavigationItem(string label, string icon, string route, string section = "body");
    }
} 