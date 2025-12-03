using System;
using Avalonia.Controls;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages.NavigationView;

public partial class LifecycleSample : UserControl
{
    private readonly string _instanceId;
    private readonly DateTime _createdAt;

    public LifecycleSample()
    {
        _instanceId = Guid.NewGuid().ToString("N")[..8];
        _createdAt = DateTime.Now;
        
        InitializeComponent();
        
        InstanceIdText.Text = _instanceId;
        CreatedAtText.Text = _createdAt.ToString("HH:mm:ss.fff");
    }
}

