using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Collections.Generic;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void DgvDemos_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PlaySelected();
        }

        private void PlaySelected()
        {
            if (dgvDemos == null || dgvDemos.SelectedItem == null)
            {
                ShowMessageDialog("Информация", "Выберите демо-файл из списка.");
                return;
            }

            DemoGridRow row = (DemoGridRow)dgvDemos.SelectedItem;
            PlaySelectedFile(row.FilePath);
        }

        public void PlaySelectedFile(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return;

            string baseDir = GetDemosBaseDir();
            // Copy selected demo to target faceit.dem in General
            string destPath = Path.Combine(Path.Combine(baseDir, "General"), "faceit.dem");
            bool copySuccess = true;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(file, destPath, true);
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("used by another process") || ioEx.Message.Contains("занят другим процессом"))
            {
                copySuccess = false;
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Ошибка подготовки запуска: " + ex.Message, true);
                return;
            }

            // Always copy command to clipboard for easy manual play
            string playCmd = "playdemo faceit_demos/General/faceit.dem";
            if (settings.EnableDemoVoice)
            {
                playCmd += "; tv_listen_voice_indices -1; tv_listen_voice_indices_h -1";
            }

            // Append active CS2 binds to clipboard
            if (settings.AutoApplyBinds && settings.Binds != null)
            {
                foreach (var b in settings.Binds)
                {
                    if (b.IsEnabled && !string.IsNullOrEmpty(b.Key) && !string.IsNullOrEmpty(b.Command))
                    {
                        playCmd += string.Format("; bind \"{0}\" \"{1}\"", b.Key, b.Command);
                    }
                }
            }

            try
            {
                Clipboard.SetText(playCmd);
            }
            catch { }

            // Check if CS2 is already running
            bool isRunning = false;
            try
            {
                Process[] pname = Process.GetProcessesByName("cs2");
                if (pname.Length > 0) isRunning = true;
            }
            catch { }

            if (isRunning)
            {
                lblStatus.Text = "Команда скопирована для запущенной CS2";
                if (!copySuccess)
                {
                    ShowMessageDialog("Игра уже запущена", "Демка готова! Команда для запуска скопирована в буфер обмена.\n\nПросто вставьте её в консоль CS2 (нажмите Ctrl+V).\n\n(Файл воспроизведения занят игрой CS2, поэтому воспроизведется старая демка, пока вы не введете 'disconnect' в консоли или не перезапустите CS2).");
                }
                else
                {
                    ShowMessageDialog("Игра уже запущена", "Демка готова! Команда для запуска скопирована в буфер обмена:\n\n" + playCmd + "\n\nПросто откройте консоль в CS2 (~) и нажмите Ctrl+V.");
                }
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo("steam://run/730") { UseShellExecute = true });
                lblStatus.Text = "Запуск CS2 через Steam...";
                if (!copySuccess)
                {
                    ShowMessageDialog("Запуск CS2", "Запускаем CS2 через Steam...\n\nКоманда для воспроизведения демки скопирована в буфер обмена. Вставьте её в консоль CS2 (нажмите Ctrl+V) после загрузки игры.\n\n(Файл воспроизведения занят, поэтому новая демка воспроизведется после ввода disconnect или перезапуска CS2).");
                }
                else
                {
                    ShowMessageDialog("Запуск CS2", "Запускаем CS2 через Steam...\n\nКоманда для воспроизведения демки скопирована в буфер обмена.\n\nПосле того как игра загрузится, откройте консоль (~) и вставьте команду (нажмите Ctrl+V).");
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка запуска", "Не удалось запустить CS2 через Steam: " + ex.Message, true);
            }
        }

        private void BtnMoveDemo_Click(object sender, RoutedEventArgs e)
        {
            if (dgvDemos.SelectedItems.Count == 0)
            {
                ShowMessageDialog("Информация", "Выберите хотя бы одну демку для переноса.");
                return;
            }

            string target = ShowSelectFolderDialog("Куда перенести выбранные демки?");
            if (string.IsNullOrEmpty(target)) return;

            List<DemoGridRow> selectedRows = new List<DemoGridRow>();
            foreach (var item in dgvDemos.SelectedItems)
            {
                selectedRows.Add((DemoGridRow)item);
            }

            MoveSelectedDemos(selectedRows, target);
        }
    }
}
