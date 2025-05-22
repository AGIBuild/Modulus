using System;
using System.ComponentModel.DataAnnotations;

namespace Modulus.App.Options
{
    /// <summary>
    /// 插件系统配置选项
    /// </summary>
    public sealed class PluginOptions
    {
        public const string SectionName = "Plugins";

        /// <summary>
        /// 安装目录下的插件路径
        /// </summary>
        [Required]
        [MinLength(1)]
        public string InstallPath { get; init; } = "Plugins";

        /// <summary>
        /// 用户数据目录下的插件路径
        /// </summary>
        [Required]
        [MinLength(1)]
        public string UserPath { get; init; } = "%APPDATA%/Modulus/Plugins";

        /// <summary>
        /// 验证插件路径是否存在
        /// </summary>
        public bool ValidateDirectoryExists { get; init; } = true;
    }
}
