using System.Collections.Generic;
using System.Windows.Input;

namespace Modulus.Plugin.Abstractions
{
    public class PluginMenuItem
    {
        public string? Header { get; set; }
        public ICommand? Command { get; set; }
        public object? CommandParameter { get; set; }
        public string? Icon { get; set; } // Could be a path or a font glyph
        public IList<PluginMenuItem>? Items { get; set; }
        public string? ToolTip { get; set; }
        public bool IsCheckable { get; set; }
        public bool IsChecked { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? InputGestureText { get; set; } // e.g., Ctrl+O
        public int DisplayOrder { get; set; } = 100;
    }
}
