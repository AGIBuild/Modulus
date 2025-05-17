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
            // Arrange: create a test plugin dll in the plugin directory
            var pluginDir = PluginLoader.GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            var testDll = Path.Combine(pluginDir, "TestPlugin.dll");
            File.WriteAllBytes(testDll, new byte[] { 0 }); // placeholder, should be a valid dll in real test

            // Act
            var loader = new PluginLoader();
            var context = loader.LoadPlugin(testDll);
            Assert.NotNull(context);
            Assert.IsType<AssemblyLoadContext>(context);

            loader.UnloadPlugin(context);
            // After unloading, context.IsAlive should be false (requires WeakReference tracking)
        }

        [Fact]
        public void Should_Reload_Plugin_On_File_Change()
        {
            // Arrange
            var loader = new PluginLoader();
            var pluginDir = PluginLoader.GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            var testDll = Path.Combine(pluginDir, "TestPlugin.dll");
            File.WriteAllBytes(testDll, new byte[] { 0 }); // placeholder
            bool reloaded = false;
            loader.PluginReloaded += (s, e) => reloaded = true;
            loader.WatchPlugins();
            // Act: simulate dll change
            File.SetLastWriteTimeUtc(testDll, DateTime.UtcNow);
            System.Threading.Thread.Sleep(300); // wait for event
            // Assert
            Assert.True(reloaded);
        }
    }
}
