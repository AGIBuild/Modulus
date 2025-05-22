using CommunityToolkit.Mvvm.ComponentModel;

namespace Modulus.App.Controls.ViewModels
{
    /// <summary>
    /// 导航项模型，支持直接通过View名称导航
    /// </summary>
    public partial class NavigationItemModel : ObservableObject
    {
        /// <summary>
        /// 导航项标签文本
        /// </summary>
        [ObservableProperty]
        private string label = string.Empty;
        
        /// <summary>
        /// 导航项图标（Segoe MDL2 Assets字体字符）
        /// </summary>
        [ObservableProperty]
        private string icon = string.Empty;
        
        /// <summary>
        /// 导航项是否处于活动状态
        /// </summary>
        [ObservableProperty]
        private bool isActive;
        
        /// <summary>
        /// 导航项是否显示徽章
        /// </summary>
        [ObservableProperty]
        private bool hasBadge;
        
        /// <summary>
        /// 徽章文本
        /// </summary>
        [ObservableProperty]
        private string badgeText = string.Empty;
        
        /// <summary>
        /// 视图名称（用于导航）
        /// </summary>
        [ObservableProperty]
        private string viewName = string.Empty;
        
        /// <summary>
        /// 导航附加参数
        /// </summary>
        [ObservableProperty]
        private object? parameter;
        
        /// <summary>
        /// 导航项所属区域（body或footer）
        /// </summary>
        [ObservableProperty]
        private string section = "body";
        
        /// <summary>
        /// 设置一个数字徽章
        /// </summary>
        /// <param name="count">显示的数字，大于99显示"99+"</param>
        public void SetBadge(int count)
        {
            if (count <= 0)
            {
                HasBadge = false;
                BadgeText = string.Empty;
                return;
            }

            HasBadge = true;
            BadgeText = count > 99 ? "99+" : count.ToString();
        }

        /// <summary>
        /// 设置一个点状徽章（无文本）
        /// </summary>
        public void SetDotBadge()
        {
            HasBadge = true;
            BadgeText = string.Empty;
        }

        /// <summary>
        /// 清除徽章
        /// </summary>
        public void ClearBadge()
        {
            HasBadge = false;
            BadgeText = string.Empty;
        }
    }
} 