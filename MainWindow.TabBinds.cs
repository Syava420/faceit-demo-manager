using System;
using System.Windows;
using System.Collections.Generic;

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

        private void BtnAddBind_Click(object sender, RoutedEventArgs e)
        {
            if (settings.Binds == null) settings.Binds = new List<DemoBind>();
            settings.Binds.Add(new DemoBind() { IsEnabled = true, ActionName = "Новое действие", Key = "", Command = "" });
            dgvBinds.ItemsSource = null;
            dgvBinds.ItemsSource = settings.Binds;
            SaveConfig();
        }

        private void BtnDeleteBind_Click(object sender, RoutedEventArgs e)
        {
            if (dgvBinds.SelectedItem == null)
            {
                ShowMessageDialog("Информация", "Выберите бинд из списка для удаления.");
                return;
            }
            
            DemoBind selected = (DemoBind)dgvBinds.SelectedItem;
            bool confirm = ShowConfirmDialog("Удаление бинда", string.Format("Вы действительно хотите удалить бинд '{0}'?", selected.ActionName));
            if (confirm)
            {
                settings.Binds.Remove(selected);
                dgvBinds.ItemsSource = null;
                dgvBinds.ItemsSource = settings.Binds;
                SaveConfig();
            }
        }
    }
}
