using System;
using Avalonia.Controls;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages;

public partial class LifecycleDemoPage : UserControl
{
    private readonly string _instanceId;
    private readonly DateTime _createdAt;

    public LifecycleDemoPage()
    {
        _instanceId = Guid.NewGuid().ToString("N")[..8];
        _createdAt = DateTime.Now;
        
        InitializeComponent();
        
        InstanceIdText.Text = _instanceId;
        CreatedAtText.Text = _createdAt.ToString("HH:mm:ss.fff");
    }
}

