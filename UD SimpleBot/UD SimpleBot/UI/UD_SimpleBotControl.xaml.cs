using System.Windows;
using System.Windows.Controls;

namespace UD_SimpleBot.UI
{
    public partial class UD_SimpleBotControl : UserControl
    {

        private SimpleBot Plugin { get; }

        private UD_SimpleBotControl()
        {
            InitializeComponent();
        }

        public UD_SimpleBotControl(SimpleBot plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin.Save();
        }
    }
}
