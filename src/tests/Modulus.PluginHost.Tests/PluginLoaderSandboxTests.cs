using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Modulus.PluginHost.Tests
{
    public class PluginLoaderSandboxTests
    {
        [Fact]
        public void Should_Only_Load_Plugin_Implementing_IPlugin()
        {
            var loader = new PluginHost.PluginLoader();
            var pluginDir = PluginHost.PluginLoader.GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            var fakeDll = Path.Combine(pluginDir, "FakeNotAPlugin.dll");
            File.WriteAllBytes(fakeDll, new byte[] { 0 }); // placeholder
            // Here we use typeof(IDisposable) instead of IPlugin, in real case use IPlugin
            Assert.Throws<InvalidOperationException>(() => loader.RunPluginSandboxed(fakeDll, typeof(IDisposable)));
        }

        [Fact]
        public void SafeRunPlugin_Should_Not_Throw_On_BadPlugin()
        {
            var loader = new PluginHost.PluginLoader();
            var pluginDir = PluginHost.PluginLoader.GetUserPluginDirectory();
            Directory.CreateDirectory(pluginDir);
            var fakeDll = Path.Combine(pluginDir, "FakeNotAPlugin.dll");
            File.WriteAllBytes(fakeDll, new byte[] { 0 }); // placeholder
            var result = loader.SafeRunPlugin(fakeDll, typeof(IDisposable));
            Assert.Null(result);
        }
    }
}
