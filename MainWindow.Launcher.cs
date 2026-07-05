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
            if (dgvDemos.SelectedItem == null)
            {
                ShowMessageDialog("Информация", "Выберите демо-файл из списка.");
                return;
            }

            DemoGridRow row = (DemoGridRow)dgvDemos.SelectedItem;
            string file = row.FilePath;

            string cs2Dir = txtCS2.Text.Trim();
            string cs2Exe = Path.Combine(Path.GetDirectoryName(cs2Dir), "bin", "win64", "cs2.exe");

            if (!File.Exists(cs2Exe))
            {
                ShowMessageDialog("Ошибка CS2", "Не найден cs2.exe по пути: " + cs2Exe, true);
                return;
            }

            string baseDir = GetDemosBaseDir();
            // Copy selected demo to target faceit.dem in General
            string destPath = Path.Combine(Path.Combine(baseDir, "General"), "faceit.dem");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(file, destPath, true);
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
            string bindArgs = "";
            if (settings.AutoApplyBinds && settings.Binds != null)
            {
                foreach (var b in settings.Binds)
                {
                    if (b.IsEnabled && !string.IsNullOrEmpty(b.Key) && !string.IsNullOrEmpty(b.Command))
                    {
                        playCmd += string.Format("; bind \"{0}\" \"{1}\"", b.Key, b.Command);
                        bindArgs += string.Format(" +bind \"{0}\" \"{1}\"", b.Key, b.Command.Replace("\"", "\\\""));
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
                ShowMessageDialog("Игра уже запущена", "Демка готова! Команда для запуска скопирована в буфер обмена:\n\n" + playCmd + "\n\nПросто откройте консоль в CS2 (~) и нажмите Ctrl+V.");
                return;
            }

            // Try to find Steam directory to launch via Steam client to avoid connection issues
            string steamPath = null;
            try
            {
                steamPath = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null) as string;
                if (string.IsNullOrEmpty(steamPath))
                {
                    steamPath = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null) as string;
                }
            }
            catch { }

            string steamExe = null;
            if (!string.IsNullOrEmpty(steamPath))
            {
                string testPath = Path.Combine(steamPath, "steam.exe");
                if (File.Exists(testPath)) steamExe = testPath;
            }

            if (string.IsNullOrEmpty(steamExe))
            {
                string[] commonPaths = new string[] {
                    @"C:\Program Files (x86)\Steam\steam.exe",
                    @"C:\Program Files\Steam\steam.exe",
                    @"D:\Steam\steam.exe",
                    @"E:\Steam\steam.exe"
                };
                foreach (string p in commonPaths)
                {
                    if (File.Exists(p)) { steamExe = p; break; }
                }
            }

            string voiceArgs = settings.EnableDemoVoice ? " +tv_listen_voice_indices -1 +tv_listen_voice_indices_h -1" : "";
            string launchArgs = voiceArgs + bindArgs;

            try
            {
                if (!string.IsNullOrEmpty(steamExe))
                {
                    string steamArgs = "-applaunch 730 -steam -game csgo +playdemo faceit_demos/General/faceit.dem" + launchArgs;
                    Process.Start(steamExe, steamArgs);
                    lblStatus.Text = "Запуск CS2 (через Steam): " + Path.GetFileName(file);
                }
                else
                {
                    string args = "-steam -game csgo +playdemo faceit_demos/General/faceit.dem" + launchArgs;
                    Process.Start(cs2Exe, args);
                    lblStatus.Text = "Запуск CS2 (напрямую): " + Path.GetFileName(file);
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Ошибка запуска процесса: " + ex.Message, true);
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
