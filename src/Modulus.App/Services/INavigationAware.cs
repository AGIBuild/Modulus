namespace Modulus.App.Services
{
    /// <summary>
    /// 导航感知接口，处理导航生命周期
    /// </summary>
    public interface INavigationAware
    {
        /// <summary>
        /// 导航到此页面时调用
        /// </summary>
        /// <param name="parameter">导航参数</param>
        void OnNavigatedTo(object? parameter);
        
        /// <summary>
        /// 从此页面导航离开时调用
        /// </summary>
        /// <returns>如果可以导航返回true，否则返回false</returns>
        bool OnNavigatingFrom();
    }
} 