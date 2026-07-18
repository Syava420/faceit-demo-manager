using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private void RefreshFolders()
        {
            if (lstFolders == null) return;
            string selected = lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;

            lstFolders.Items.Clear();
            lstFolders.Items.Add(new FolderItem { DisplayName = "[Все демки]", RelativePath = "[Все демки]", Depth = 0 });
            
            string baseDir = GetDemosBaseDir();
            if (Directory.Exists(baseDir))
            {
                string genDir = Path.Combine(baseDir, "General");
                if (!Directory.Exists(genDir))
                {
                    try { Directory.CreateDirectory(genDir); } catch { }
                }

                List<FolderItem> folderItems = new List<FolderItem>();
                LoadSubfoldersRecursive(baseDir, "", 0, folderItems);
                foreach (var item in folderItems)
                {
                    lstFolders.Items.Add(item);
                }
            }

            if (selected != null)
            {
                bool found = false;
                foreach (var item in lstFolders.Items)
                {
                    if (item is FolderItem fi && fi.RelativePath == selected)
                    {
                        lstFolders.SelectedItem = fi;
                        found = true;
                        break;
                    }
                }
                if (!found) lstFolders.SelectedIndex = 0;
            }
            else
            {
                lstFolders.SelectedIndex = 0;
            }
        }

        private void LoadSubfoldersRecursive(string baseDir, string relativePath, int depth, List<FolderItem> result)
        {
            LibraryFileService.LoadSubfoldersRecursive(baseDir, relativePath, depth, result, this.collapsedFolders);
        }

        private void UpdateImportFolderCombobox()
        {
            if (CboImportFolder == null) return;
            string selected = CboImportFolder.SelectedItem != null ? CboImportFolder.SelectedItem.ToString() : null;
            CboImportFolder.Items.Clear();
            CboImportFolder.Items.Add("General");

            string baseDir = GetDemosBaseDir();
            if (Directory.Exists(baseDir))
            {
                List<FolderItem> folderItems = new List<FolderItem>();
                LoadSubfoldersRecursive(baseDir, "", 0, folderItems);
                foreach (var item in folderItems)
                {
                    if (!item.RelativePath.Equals("General", StringComparison.OrdinalIgnoreCase))
                    {
                        CboImportFolder.Items.Add(item.RelativePath);
                    }
                }
            }

            if (selected != null && CboImportFolder.Items.Contains(selected))
            {
                CboImportFolder.SelectedItem = selected;
            }
            else
            {
                CboImportFolder.SelectedIndex = 0;
            }
        }

        private int currentLoadId = 0;

        private void RefreshDemoList()
        {
            if (dgvDemos == null) return;

            string selectedFolder = lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (selectedFolder == null)
            {
                dgvDemos.ItemsSource = null;
                return;
            }

            if (selectedFolder == "[Все демки]") selectedFolder = "[All Demos]";

            string baseDir = GetDemosBaseDir();
            if (!Directory.Exists(baseDir))
            {
                dgvDemos.ItemsSource = null;
                return;
            }

            string filter = txtSearch.Text.Trim();
            string mapFilter = selectedMapFilter;
            int loadId = ++currentLoadId;

            if (lblStatus != null)
            {
                lblStatus.Text = "Загрузка матчей...";
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var localDb = new Dictionary<string, DemoMetadata>(StringComparer.OrdinalIgnoreCase);
                    DemoProcessor.LoadMetadataDb(baseDir, localDb);

                    List<string> demoFiles = new List<string>();
                    if (selectedFolder == "[All Demos]")
                    {
                        demoFiles.AddRange(Directory.GetFiles(baseDir, "*.dem", SearchOption.AllDirectories));
                    }
                    else
                    {
                        string targetDir = Path.Combine(baseDir, selectedFolder);
                        if (Directory.Exists(targetDir))
                        {
                            demoFiles.AddRange(Directory.GetFiles(targetDir, "*.dem"));
                        }
                    }

                    // Gather all unique maps present in current list of files
                    HashSet<string> uniqueMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string file in demoFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName.Equals("faceit.dem", StringComparison.OrdinalIgnoreCase)) continue;
                        string relativePath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                        DemoMetadata dm;
                        if (!localDb.TryGetValue(relativePath, out dm))
                        {
                            dm = DemoProcessor.ParseMetadataFromFilename(fileName);
                        }
                        if (dm != null && !string.IsNullOrEmpty(dm.Map) && !dm.Map.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                        {
                            uniqueMaps.Add(dm.Map);
                        }
                    }

                    List<DemoGridRow> gridList = new List<DemoGridRow>();
                    foreach (string file in demoFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        if (fileName.Equals("faceit.dem", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip layout demo
                        }
                        string relativePath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                        string folderName = Path.GetFileName(Path.GetDirectoryName(file));

                        DemoMetadata dm = null;
                        if (!localDb.TryGetValue(relativePath, out dm))
                        {
                            dm = DemoProcessor.ParseMetadataFromFilename(fileName);
                        }

                        // Apply text filter
                        if (!string.IsNullOrEmpty(filter))
                        {
                            bool match = (dm.Map != null && dm.Map.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                         (dm.Note != null && dm.Note.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                         (dm.Date != null && dm.Date.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                         (folderName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (!match) continue;
                        }

                        // Apply map button filter
                        if (!string.IsNullOrEmpty(mapFilter))
                        {
                            if (!string.Equals(dm.Map, mapFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        gridList.Add(DemoGridRow.FromMetadata(dm, file, baseDir));
                    }

                    // Sort by Import Date descending (most recently added demos at the top) by default
                    gridList.Sort((a, b) => b.ImportDate.CompareTo(a.ImportDate));

                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // Ensure we discard out-of-order updates
                        if (loadId != currentLoadId) return;

                        this.metadataDb = localDb;
                        dgvDemos.ItemsSource = gridList;

                        // Dynamically rebuild map filter buttons
                        if (pnlMapFilters != null)
                        {
                            pnlMapFilters.Children.Clear();
                            pnlMapFilters.Children.Add(CreateMapFilterButton(null, mapFilter == null));
                            foreach (string map in uniqueMaps)
                            {
                                pnlMapFilters.Children.Add(CreateMapFilterButton(map, string.Equals(mapFilter, map, StringComparison.OrdinalIgnoreCase)));
                            }
                        }

                        if (lblStatus != null)
                        {
                            lblStatus.Text = "Готов к работе";
                        }
                    }));
                }
                catch (Exception ex)
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (loadId != currentLoadId) return;
                        if (lblStatus != null)
                        {
                            lblStatus.Text = "Ошибка загрузки списка: " + ex.Message;
                        }
                    }));
                }
            });
        }

        private Button CreateMapFilterButton(string mapName, bool isActive)
        {
            Button btn = new Button();
            btn.Content = (mapName == null) ? "Все карты" : mapName;
            
            try
            {
                Style style = null;
                FrameworkElement contentElement = this.Content as FrameworkElement;
                if (contentElement != null)
                {
                    style = contentElement.TryFindResource(isActive ? "MapFilterBtnActiveStyle" : "MapFilterBtnStyle") as Style;
                }
                if (style != null)
                {
                    btn.Style = style;
                }
            }
            catch { }

            btn.Click += (s, e) => {
                selectedMapFilter = mapName;
                RefreshDemoList();
            };
            return btn;
        }

        private void BtnNewCategory_Click(object sender, RoutedEventArgs e)
        {
            string current = lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            bool isSubfolder = false;
            string parentPath = "";
            string prompt = "Введите имя новой папки:";
            
            if (!string.IsNullOrEmpty(current) && current != "[Все демки]" && current != "General")
            {
                parentPath = current;
                isSubfolder = true;
                prompt = string.Format("Создание подпапки в '{0}'.\nВведите имя новой папки:", current);
            }
            
            string input = ShowInputDialog(isSubfolder ? "Новая подпапка" : "Новая папка", prompt);
            if (string.IsNullOrEmpty(input)) return;

            string name = Regex.Replace(input, @"[\\/:*?""<>|]", "").Trim();
            if (string.IsNullOrEmpty(name)) return;

            string baseDir = GetDemosBaseDir();
            if (string.IsNullOrEmpty(baseDir))
            {
                ShowMessageDialog("Ошибка", "Ошибка: проверьте путь к CS2.", true);
                return;
            }

            string relativePath = isSubfolder ? parentPath + "/" + name : name;
            if (LibraryFileService.CreateFolder(baseDir, relativePath))
            {
                RefreshFolders();
                UpdateImportFolderCombobox();
                
                // Select the newly created folder
                foreach (var item in lstFolders.Items)
                {
                    if (item is FolderItem fi && fi.RelativePath == relativePath)
                    {
                        lstFolders.SelectedItem = fi;
                        break;
                    }
                }
            }
            else
            {
                ShowMessageDialog("Ошибка", "Не удалось создать папку.", true);
            }
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            string current = lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (string.IsNullOrEmpty(current) || current == "[Все демки]" || current == "General")
            {
                ShowMessageDialog("Предупреждение", "Эту папку нельзя удалить.", true);
                return;
            }

            bool mr = ShowConfirmDialog("Удалить папку", string.Format("Вы действительно хотите удалить папку '{0}' и её подпапки? Демки внутри останутся на диске.", current));
            if (!mr) return;

            string baseDir = GetDemosBaseDir();
            if (LibraryFileService.DeleteCategoryFolder(baseDir, current, this.metadataDb))
            {
                RefreshFolders();
                UpdateImportFolderCombobox();
            }
            else
            {
                ShowMessageDialog("Ошибка", "Не удалось удалить папку.", true);
            }
        }

        private void LstFolders_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DependencyObject k = e.OriginalSource as DependencyObject;
            while (k != null && !(k is ListBoxItem))
            {
                k = VisualTreeHelper.GetParent(k);
            }

            if (k is ListBoxItem listBoxItem && listBoxItem.DataContext is FolderItem folderItem)
            {
                lstFolders.SelectedItem = folderItem;

                ContextMenu menu = new ContextMenu();
                menu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181b"));
                menu.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272a"));
                menu.Foreground = Brushes.White;

                MenuItem createItem = new MenuItem { Header = "Создать подпапку", Foreground = Brushes.White };
                createItem.Click += (s, ev) => BtnNewCategory_Click(null, null);

                MenuItem deleteItem = new MenuItem { Header = "Удалить папку", Foreground = Brushes.White };
                deleteItem.Click += (s, ev) => BtnDeleteCategory_Click(null, null);

                if (folderItem.RelativePath == "[Все демки]" || folderItem.RelativePath == "General")
                {
                    deleteItem.IsEnabled = false;
                }

                menu.Items.Add(createItem);
                menu.Items.Add(deleteItem);

                listBoxItem.ContextMenu = menu;
            }
            else
            {
                ContextMenu menu = new ContextMenu();
                menu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181b"));
                menu.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272a"));
                menu.Foreground = Brushes.White;

                MenuItem createItem = new MenuItem { Header = "Создать папку", Foreground = Brushes.White };
                createItem.Click += (s, ev) => BtnNewCategory_Click(null, null);

                menu.Items.Add(createItem);
                lstFolders.ContextMenu = menu;
            }
        }

        private void LstFolders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstFolders.SelectedItem is FolderItem fi)
            {
                if (fi.RelativePath == "[Все демки]" || fi.RelativePath == "General")
                    return;

                string fullPath = Path.Combine(GetDemosBaseDir(), fi.RelativePath);
                if (Directory.Exists(fullPath) && Directory.GetDirectories(fullPath).Length > 0)
                {
                    if (collapsedFolders.Contains(fi.RelativePath))
                    {
                        collapsedFolders.Remove(fi.RelativePath);
                    }
                    else
                    {
                        collapsedFolders.Add(fi.RelativePath);
                    }
                    RefreshFolders();
                }
            }
        }



        private void DgvDemos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvDemos.SelectedItem == null)
            {
                isUpdatingFields = true;
                txtEditMap.Text = "";
                txtEditScore.Text = "";
                txtEditKD.Text = "";
                txtEditDate.Text = "";
                txtNoteEdit.Text = "";

                txtEditMap.IsEnabled = false;
                txtEditScore.IsEnabled = false;
                txtEditKD.IsEnabled = false;
                txtEditDate.IsEnabled = false;
                txtNoteEdit.IsEnabled = false;
                isUpdatingFields = false;
                return;
            }

            txtEditMap.IsEnabled = true;
            txtEditScore.IsEnabled = true;
            txtEditKD.IsEnabled = true;
            txtEditDate.IsEnabled = true;
            txtNoteEdit.IsEnabled = true;

            DemoGridRow row = (DemoGridRow)dgvDemos.SelectedItem;
            string file = row.FilePath;
            string baseDir = GetDemosBaseDir();
            string relPath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');

            DemoMetadata dm = null;
            if (!metadataDb.TryGetValue(relPath, out dm))
            {
                dm = DemoProcessor.ParseMetadataFromFilename(Path.GetFileName(file));
            }

            isUpdatingFields = true;
            txtEditMap.Text = dm.Map;
            txtEditScore.Text = dm.Score;
            txtEditKD.Text = dm.KD;
            txtEditDate.Text = dm.Date;
            txtNoteEdit.Text = dm.Note;
            isUpdatingFields = false;
        }

        private void EditField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingFields) return;
            if (dgvDemos.SelectedItem == null) return;

            DemoGridRow row = (DemoGridRow)dgvDemos.SelectedItem;
            string file = row.FilePath;
            string baseDir = GetDemosBaseDir();
            string relPath = file.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');

            DemoMetadata dm = null;
            if (!metadataDb.TryGetValue(relPath, out dm))
            {
                dm = DemoProcessor.ParseMetadataFromFilename(Path.GetFileName(file));
            }

            dm.Map = txtEditMap.Text.Trim();
            dm.Score = txtEditScore.Text.Trim();
            dm.KD = txtEditKD.Text.Trim();
            dm.Date = txtEditDate.Text.Trim();
            dm.Note = txtNoteEdit.Text.Trim();

            DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, relPath, dm);

            // Update row properties
            row.Map = DemoGridRow.GetMapEmoji(dm.Map) + " " + dm.Map;
            row.Score = dm.Score;
            row.Date = dm.Date;
            row.Note = dm.Note;

            // Update row properties
            var tempRow = DemoGridRow.FromMetadata(dm, file, baseDir);
            row.Map = tempRow.Map;
            row.Score = tempRow.Score;
            row.ScoreText = tempRow.ScoreText;
            row.IsWin = tempRow.IsWin;
            row.KDA = tempRow.KDA;
            row.KDRatio = tempRow.KDRatio;
            row.KDStatus = tempRow.KDStatus;
            row.ADR = tempRow.ADR;
            row.Date = tempRow.Date;
            row.DateFormatted = tempRow.DateFormatted;
            row.Note = tempRow.Note;

            dgvDemos.Items.Refresh();
        }

        private void BtnDeleteDemo_Click(object sender, RoutedEventArgs e)
        {
            if (dgvDemos.SelectedItems.Count == 0)
            {
                ShowMessageDialog("Информация", "Выберите хотя бы одну демку для удаления.");
                return;
            }

            bool mr = ShowConfirmDialog("Удаление файлов", string.Format("Вы уверены, что хотите навсегда удалить выбранные демки ({0} шт.)?", dgvDemos.SelectedItems.Count));
            if (!mr) return;

            string baseDir = GetDemosBaseDir();
            int deletedCount = 0;

            List<DemoGridRow> toDelete = new List<DemoGridRow>();
            foreach (var item in dgvDemos.SelectedItems)
            {
                toDelete.Add((DemoGridRow)item);
            }

            foreach (var row in toDelete)
            {
                try
                {
                    if (File.Exists(row.FilePath))
                    {
                        File.Delete(row.FilePath);
                    }
                    string relPath = row.FilePath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    if (metadataDb.ContainsKey(relPath))
                    {
                        metadataDb.Remove(relPath);
                    }
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    ShowMessageDialog("Ошибка", string.Format("Не удалось удалить файл {0}: {1}", Path.GetFileName(row.FilePath), ex.Message), true);
                }
            }

            // Save cleaned metadata db
            if (deletedCount > 0)
            {
                try
                {
                    string path = Path.Combine(baseDir, "metadata.txt");
                    List<string> lines = new List<string>();
                    foreach (var kvp in metadataDb)
                    {
                        lines.Add(string.Format("{0}|{1}|{2}|{3}|{4}|{5}", kvp.Key, kvp.Value.Map, kvp.Value.Score, kvp.Value.KD, kvp.Value.Date, kvp.Value.Note));
                    }
                    File.WriteAllLines(path, lines);
                }
                catch { }

                RefreshDemoList();
                lblStatus.Text = string.Format("Успешно удалено файлов: {0}", deletedCount);
            }
        }

        private string GetDemosBaseDir()
        {
            string cs2Dir = "";
            if (txtCS2 != null)
            {
                if (Thread.CurrentThread == this.Dispatcher.Thread)
                {
                    cs2Dir = txtCS2.Text.Trim();
                }
                else
                {
                    this.Dispatcher.Invoke(new Action(() => {
                        cs2Dir = txtCS2.Text.Trim();
                    }));
                }
            }
            if (string.IsNullOrEmpty(cs2Dir) && settings != null)
            {
                cs2Dir = settings.CS2Path;
            }
            if (string.IsNullOrEmpty(cs2Dir)) return "";
            return Path.Combine(cs2Dir, "faceit_demos");
        }
    }
}
