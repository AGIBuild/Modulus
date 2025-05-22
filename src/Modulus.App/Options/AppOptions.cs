using System;
using System.ComponentModel.DataAnnotations;

namespace Modulus.App.Options
{
    /// <summary>
    /// 应用程序配置选项
    /// </summary>
    public sealed class AppOptions
    {
        public const string SectionName = "App";

        /// <summary>
        /// 应用程序名称
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Name { get; init; } = "Modulus";

        /// <summary>
        /// 应用程序版本
        /// </summary>
        [Required]
        [RegularExpression(@"^\d+\.\d+\.\d+(?:\-.+)?$", ErrorMessage = "Version must be in format X.Y.Z or X.Y.Z-suffix")]
        public string Version { get; init; } = "1.0.0";
    }
}
