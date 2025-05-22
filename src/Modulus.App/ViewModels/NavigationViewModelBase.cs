using CommunityToolkit.Mvvm.ComponentModel;
using Modulus.App.Services;
using System;

namespace Modulus.App.ViewModels
{
    /// <summary>
    /// 导航ViewModel基类，提供导航生命周期支持
    /// </summary>
    public abstract class NavigationViewModelBase : ObservableObject, INavigationAware
    {
        private readonly INavigationService? _navigationService;
        
        /// <summary>
        /// 视图名称，用于导航识别
        /// </summary>
        public abstract string ViewName { get; }
        
        /// <summary>
        /// 创建导航ViewModel基类实例
        /// </summary>
        /// <param name="navigationService">导航服务</param>
        protected NavigationViewModelBase(INavigationService? navigationService = null)
        {
            _navigationService = navigationService;
            
            // 如果提供了导航服务，注册导航事件处理
            if (_navigationService != null)
            {
                _navigationService.RegisterNavigatedToHandler(ViewName, OnNavigatedTo);
                _navigationService.RegisterNavigatedFromHandler(ViewName, OnNavigatingFrom);
            }
        }
        
        /// <summary>
        /// 导航到该页面时触发
        /// </summary>
        /// <param name="parameter">导航参数</param>
        public virtual void OnNavigatedTo(object? parameter)
        {
            // 默认实现为空，子类可以重写以处理导航到事件
        }
        
        /// <summary>
        /// 从该页面导航离开时触发
        /// </summary>
        /// <returns>如果可以离开返回true，否则返回false</returns>
        public virtual bool OnNavigatingFrom()
        {
            // 默认允许离开，子类可以重写以处理导航离开事件
            return true;
        }
        
        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="viewName">视图名称</param>
        /// <param name="parameter">导航参数</param>
        protected void NavigateTo(string viewName, object? parameter = null)
        {
            _navigationService?.NavigateTo(viewName, parameter);
        }
        
        /// <summary>
        /// 导航回退
        /// </summary>
        /// <returns>如果成功返回true，否则返回false</returns>
        protected bool GoBack()
        {
            return _navigationService?.GoBack() ?? false;
        }
        
        /// <summary>
        /// 导航前进
        /// </summary>
        /// <returns>如果成功返回true，否则返回false</returns>
        protected bool GoForward()
        {
            return _navigationService?.GoForward() ?? false;
        }
        
        /// <summary>
        /// 检查是否可以回退
        /// </summary>
        protected bool CanGoBack => _navigationService?.CanGoBack ?? false;
        
        /// <summary>
        /// 检查是否可以前进
        /// </summary>
        protected bool CanGoForward => _navigationService?.CanGoForward ?? false;
    }
} 