using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private void DragDropZone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Faceit Demo Files (*.dem.zst;*.dem)|*.dem.zst;*.dem";
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                ProcessManualFiles(dialog.FileNames);
            }
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ProcessManualFiles(files);
            }
        }

        private string DetermineImportFolder(string fileName)
        {
            string mode = "General";
            string specificFolder = "General";

            if (CboImportMode != null)
            {
                this.Dispatcher.Invoke(new Action(() => {
                    var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
                    if (selectedItem != null) mode = selectedItem.Tag.ToString();
                    specificFolder = CboImportFolder != null && CboImportFolder.SelectedItem != null 
                        ? CboImportFolder.SelectedItem.ToString() 
                        : "General";
                }));
            }
            else if (settings != null)
            {
                mode = settings.ImportMode ?? "General";
                specificFolder = settings.TargetImportFolder ?? "General";
            }

            if (mode == "General") return "General";
            if (mode == "Specific" || mode == "TargetFolder") return specificFolder;

            // Ask mode (Interactive)
            string chosen = this.Dispatcher.Invoke(() => ShowSelectFolderDialog("Куда распаковать демку: " + Path.GetFileName(fileName)));
            if (string.IsNullOrEmpty(chosen)) return null; // Abort
            return chosen;
        }

        private void ProcessManualFiles(string[] files)
        {
            SwitchTab(0);
            
            string cs2 = txtCS2 != null ? txtCS2.Text.Trim() : (settings != null ? settings.CS2Path : "");

            if (btnProcess != null) btnProcess.IsEnabled = false;
            AppendLog("Начало обработки файлов...");

            Thread thread = new Thread(() =>
            {
                int successCount = 0;
                int totalCount = 0;
                foreach (string file in files)
                {
                    if (file.EndsWith(".zst", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".dem", StringComparison.OrdinalIgnoreCase))
                    {
                        totalCount++;
                        string targetCategory = DetermineImportFolder(file);
                        if (targetCategory == null)
                        {
                            AppendLog("Пропущен файл (отмена выбора папки): " + Path.GetFileName(file));
                            continue;
                        }

                        // Determine nickname contextually
                        string nickForImport = "";
                        if (targetCategory == "General")
                        {
                            nickForImport = settings.Nickname;
                        }
                        else
                        {
                            nickForImport = GetFolderNicknameRecursive(targetCategory);
                        }

                        try
                        {
                            Action<string, bool> logCallback = (msg, isErr) => {
                                AppendLog("Процессор демок: " + msg);
                            };
                            if (DemoProcessor.ProcessSingleFile(file, targetCategory, cs2, nickForImport, metadataDb, logCallback, settings.DeleteArchivesAfterUnpack))
                            {
                                successCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendLog("Ошибка обработки файла " + Path.GetFileName(file) + ": " + ex.Message);
                        }
                    }
                }

                this.Dispatcher.BeginInvoke(new Action(() => {
                    if (btnProcess != null) btnProcess.IsEnabled = true;
                    AppendLog(string.Format("Обработка завершена. Успешно импортировано: {0}/{1}", successCount, totalCount));
                    RefreshFolders();
                    RefreshDemoList();
                }));
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public void ImportSingleFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                ProcessManualFiles(new[] { filePath });
            }
        }

        public void ProcessDownloadsFolderLogic()
        {
            string downloads = settings != null && !string.IsNullOrEmpty(settings.DownloadsPath) ? settings.DownloadsPath : (txtDownloads != null ? txtDownloads.Text.Trim() : "");
            if (!Directory.Exists(downloads))
            {
                AppendLog("Ошибка: Неверный путь к папке загрузок!");
                return;
            }

            string[] files = Directory.GetFiles(downloads, "*.dem.zst");
            if (files.Length == 0)
            {
                AppendLog("В папке загрузок не найдено новых файлов *.dem.zst");
                return;
            }

            ProcessManualFiles(files);
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            ProcessDownloadsFolderLogic();
        }

        private void ToggleWatcherSetting(bool enabled)
        {
            if (dlWatcher != null)
            {
                dlWatcher.EnableRaisingEvents = enabled;
            }
        }

        private void InitializeWatcher()
        {
            try
            {
                string downloads = txtDownloads != null ? txtDownloads.Text.Trim() : (settings != null ? settings.DownloadsPath : "");
                if (Directory.Exists(downloads))
                {
                    dlWatcher = new FileSystemWatcher(downloads, "*.dem.zst");
                    dlWatcher.Created += DlWatcher_Created;
                    bool isChecked = ChkWatchFolder != null ? ChkWatchFolder.IsChecked == true : (settings != null ? settings.WatchFolder : false);
                    dlWatcher.EnableRaisingEvents = isChecked;
                }
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка авто-сканера: " + ex.Message);
            }
        }

        private void DlWatcher_Created(object sender, FileSystemEventArgs e)
        {
            AppendLog("Обнаружен файл загрузки: " + e.Name);
            Thread.Sleep(1000); // Wait for file handle to release
            
            string cs2 = "";
            this.Dispatcher.Invoke(new Action(() => {
                cs2 = txtCS2 != null ? txtCS2.Text.Trim() : (settings != null ? settings.CS2Path : "");
            }));

            string targetCategory = DetermineImportFolder(e.FullPath);
            if (targetCategory == null)
            {
                AppendLog("Отмена авто-импорта (папка не выбрана): " + e.Name);
                return;
            }

            // Determine nickname contextually for auto-scanned import
            string nickForImport = "";
            if (targetCategory == "General")
            {
                nickForImport = settings.Nickname;
            }
            else
            {
                nickForImport = GetFolderNicknameRecursive(targetCategory);
            }

            try
            {
                Action<string, bool> logCallback = (msg, isErr) => {
                    AppendLog("Авто-процессор: " + msg);
                };
                DemoProcessor.ProcessSingleFile(e.FullPath, targetCategory, cs2, nickForImport, metadataDb, logCallback, settings.DeleteArchivesAfterUnpack);
            }
            catch (Exception ex)
            {
                AppendLog("Ошибка авто-процессора: " + ex.Message);
            }
        }
    }
}
