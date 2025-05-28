using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Modulus.App.Controls
{
    /// <summary>
    /// 导航头部控件，包含导航栏展开/折叠按钮
    /// </summary>
    public partial class NavigationHeader : UserControl
    {
        public NavigationHeader()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}