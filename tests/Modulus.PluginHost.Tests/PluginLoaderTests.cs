using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace Modulus.PluginHost.Tests
{
    public class PluginLoaderTests
    {
        [Fact]
        public void Should_Find_Plugins_In_UserDirectory()
        {
            var userPluginDir = PluginLoader.GetUserPluginDirectory();
            Assert.True(userPluginDir.Contains(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        }

        [Fact]
        public void Should_Load_And_Unload_Plugin_Assembly()
        {
            // Arrange: 假设有一个测试插件dll放在插件目录
            var pluginDir = PluginLoader.GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            var testDll = Path.Combine(pluginDir, "TestPlugin.dll");
            File.WriteAllBytes(testDll, new byte[] { 0 }); // 占位符，实际测试应为有效dll

            // Act
            var loader = new PluginLoader();
            var context = loader.LoadPlugin(testDll);
            Assert.NotNull(context);
            Assert.IsType<AssemblyLoadContext>(context);

            loader.UnloadPlugin(context);
            // 卸载后 context.IsAlive 应为 false（需弱引用追踪）
        }

        [Fact]
        public void Should_Reload_Plugin_On_File_Change()
        {
            // Arrange
            var loader = new PluginLoader();
            var pluginDir = PluginLoader.GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            var testDll = Path.Combine(pluginDir, "TestPlugin.dll");
            File.WriteAllBytes(testDll, new byte[] { 0 }); // 占位符
            bool reloaded = false;
            loader.PluginReloaded += (s, e) => reloaded = true;
            loader.WatchPlugins();
            // Act: 模拟dll变化
            File.SetLastWriteTimeUtc(testDll, DateTime.UtcNow);
            System.Threading.Thread.Sleep(300); // 等待事件触发
            // Assert
            Assert.True(reloaded);
        }
    }
}
