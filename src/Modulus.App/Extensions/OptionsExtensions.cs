using System;
using System.IO;
using Modulus.App.Options;

namespace Modulus.App.Extensions
{
    public static class OptionsExtensions
    {
        /// <summary>
        /// 获取已处理环境变量的插件安装路径
        /// </summary>
        public static string GetResolvedInstallPath(this PluginOptions options)
        {
            var path = Environment.ExpandEnvironmentVariables(options.InstallPath);
            return Path.IsPathRooted(path) 
                ? path 
                : Path.Combine(AppContext.BaseDirectory, path);
        }

        /// <summary>
        /// 获取已处理环境变量的用户插件路径
        /// </summary>
        public static string GetResolvedUserPath(this PluginOptions options)
        {
            return Environment.ExpandEnvironmentVariables(options.UserPath);
        }

        /// <summary>
        /// 确保插件目录存在
        /// </summary>
        public static void EnsurePluginDirectoriesExist(this PluginOptions options)
        {
            var installPath = options.GetResolvedInstallPath();
            var userPath = options.GetResolvedUserPath();

            if (!Directory.Exists(installPath))
                Directory.CreateDirectory(installPath);

            if (!Directory.Exists(userPath))
                Directory.CreateDirectory(userPath);
        }
    }
}
