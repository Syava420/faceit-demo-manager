using System;
using System.Windows;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private void BtnResetBinds_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = ShowConfirmDialog("Сброс биндов", "Вы действительно хотите сбросить все горячие клавиши к настройкам по умолчанию?");
            if (confirm)
            {
                settings.Binds = ConfigManager.GetDefaultBinds();
                dgvBinds.ItemsSource = null;
                dgvBinds.ItemsSource = settings.Binds;
                SaveConfig();
            }
        }
    }
}
