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
            lstFolders.Items.Add("[Все демки]");
            
            string baseDir = GetDemosBaseDir();
            if (Directory.Exists(baseDir))
            {
                string genDir = Path.Combine(baseDir, "General");
                if (!Directory.Exists(genDir))
                {
                    try { Directory.CreateDirectory(genDir); } catch { }
                }

                foreach (string d in Directory.GetDirectories(baseDir))
                {
                    string name = Path.GetFileName(d);
                    lstFolders.Items.Add(name);
                }
            }

            if (selected != null && lstFolders.Items.Contains(selected))
            {
                lstFolders.SelectedItem = selected;
            }
            else
            {
                lstFolders.SelectedIndex = 0;
            }
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
                string[] subdirs = Directory.GetDirectories(baseDir);
                foreach (string subdir in subdirs)
                {
                    string name = Path.GetFileName(subdir);
                    if (!name.Equals("General", StringComparison.OrdinalIgnoreCase))
                    {
                        CboImportFolder.Items.Add(name);
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

                        // Win/Loss and Score
                        bool isWin = false;
                        string scoreText = dm.Score;
                        if (!string.IsNullOrEmpty(dm.Score))
                        {
                            string[] parts = dm.Score.Split('-');
                            if (parts.Length == 2)
                            {
                                int s1, s2;
                                if (int.TryParse(parts[0].Trim(), out s1) && int.TryParse(parts[1].Trim(), out s2))
                                {
                                    if (s1 > s2)
                                    {
                                        isWin = true;
                                        scoreText = "W " + s1 + " : " + s2;
                                    }
                                    else
                                    {
                                        isWin = false;
                                        scoreText = "L " + s1 + " : " + s2;
                                    }
                                }
                            }
                        }

                        // Stats: K/D/A and ADR
                        string kdaText = "-";
                        string kdRatioText = "-";
                        string kdStatusText = "Normal";
                        string adrText = "-";

                        if (!string.IsNullOrEmpty(dm.KD) && dm.KD != "-")
                        {
                            Match mRatio = Regex.Match(dm.KD, @"^([\d.]+)");
                            if (mRatio.Success)
                            {
                                kdRatioText = mRatio.Groups[1].Value;
                                double ratioVal;
                                if (double.TryParse(kdRatioText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ratioVal))
                                {
                                    kdStatusText = ratioVal >= 1.0 ? "High" : "Low";
                                }
                            }

                            Match mKda = Regex.Match(dm.KD, @"\(([^()]+)\)");
                            if (mKda.Success)
                            {
                                string kdaRaw = mKda.Groups[1].Value;
                                string[] kparts = kdaRaw.Split('/');
                                if (kparts.Length == 3)
                                {
                                    kdaText = string.Format("{0} / {1} / {2}", kparts[0], kparts[1], kparts[2]);
                                }
                                else if (kparts.Length == 2)
                                {
                                    kdaText = string.Format("{0} / {1} / -", kparts[0], kparts[1]);
                                }
                            }

                            Match mAdrVal = Regex.Match(dm.KD, @"\[([^[\]]+)\]");
                            if (mAdrVal.Success)
                            {
                                adrText = mAdrVal.Groups[1].Value;
                            }
                        }

                        // Date
                        string dateTextFormatted = dm.Date;
                        DateTime dtVal;
                        if (DateTime.TryParse(dm.Date, out dtVal))
                        {
                            dateTextFormatted = dtVal.ToString("ddd d MMM", new System.Globalization.CultureInfo("ru-RU")) + "\n" + dtVal.ToString("HH:mm");
                        }

                        // Import Date
                        DateTime importDt = DateTime.Now;
                        try
                        {
                            importDt = File.GetCreationTime(file);
                        }
                        catch { }
                        string importDateFormatted = importDt.ToString("ddd d MMM", new System.Globalization.CultureInfo("ru-RU")) + "\n" + importDt.ToString("HH:mm");

                        gridList.Add(new DemoGridRow()
                        {
                            Check = false,
                            Map = GetMapEmoji(dm.Map) + " " + dm.Map,
                            Score = dm.Score,
                            ScoreText = scoreText,
                            IsWin = isWin,
                            KDA = kdaText,
                            KDRatio = kdRatioText,
                            KDStatus = kdStatusText,
                            ADR = adrText,
                            Date = dm.Date,
                            DateFormatted = dateTextFormatted,
                            ImportDate = importDt,
                            ImportDateFormatted = importDateFormatted,
                            Folder = folderName.Equals("General", StringComparison.OrdinalIgnoreCase) ? "Общая" : folderName,
                            Note = dm.Note,
                            FilePath = file
                        });
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
            
            // Resolve XAML style starting from the root Border content
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

            btn.Click += (s, e) => {
                selectedMapFilter = mapName;
                RefreshDemoList();
            };
            return btn;
        }

        private string GetMapEmoji(string map)
        {
            if (string.IsNullOrEmpty(map)) return "🗺️";
            string name = map.Trim().ToLower();
            if (name.Contains("mirage")) return "🏜️";
            if (name.Contains("dust")) return "🏜️";
            if (name.Contains("ancient")) return "🌴";
            if (name.Contains("nuke")) return "☢️";
            if (name.Contains("inferno")) return "🔥";
            if (name.Contains("anubis")) return "🦂";
            if (name.Contains("vertigo")) return "🏗️";
            if (name.Contains("overpass")) return "🌉";
            return "🗺️";
        }

        private void BtnNewCategory_Click(object sender, RoutedEventArgs e)
        {
            string input = ShowInputDialog("Новая папка", "Введите имя новой папки:");
            if (string.IsNullOrEmpty(input)) return;

            string name = Regex.Replace(input, @"[\\/:*?""<>|]", "").Trim();
            if (string.IsNullOrEmpty(name)) return;

            string baseDir = GetDemosBaseDir();
            if (string.IsNullOrEmpty(baseDir))
            {
                ShowMessageDialog("Ошибка", "Ошибка: проверьте путь к CS2.", true);
                return;
            }

            string targetDir = Path.Combine(baseDir, name);
            try
            {
                Directory.CreateDirectory(targetDir);
                RefreshFolders();
                UpdateImportFolderCombobox();
                lstFolders.SelectedItem = name;
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Не удалось создать папку: " + ex.Message, true);
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

            bool mr = ShowConfirmDialog("Удалить папку", string.Format("Вы действительно хотите удалить папку '{0}'? Демки внутри нее останутся на диске.", current));
            if (!mr) return;

            string baseDir = GetDemosBaseDir();
            string targetDir = Path.Combine(baseDir, current);
            string genDir = Path.Combine(baseDir, "General");

            try
            {
                if (Directory.Exists(targetDir))
                {
                    // Move files to General folder to avoid deleting demos
                    foreach (string file in Directory.GetFiles(targetDir, "*.dem"))
                    {
                        string dest = Path.Combine(genDir, Path.GetFileName(file));
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(file, dest);
                        
                        // Update metadata path
                        string oldRel = current + "/" + Path.GetFileName(file);
                        string newRel = "General/" + Path.GetFileName(file);
                        DemoMetadata dm;
                        if (metadataDb.TryGetValue(oldRel, out dm))
                        {
                            metadataDb.Remove(oldRel);
                            DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, newRel, dm);
                        }
                    }
                    Directory.Delete(targetDir, true);
                }

                RefreshFolders();
                UpdateImportFolderCombobox();
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Ошибка при удалении: " + ex.Message, true);
            }
        }

        private Point dragStartPoint;
        private void DgvDemos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
        }

        private void DgvDemos_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (dgvDemos.SelectedItems.Count > 0)
                    {
                        List<DemoGridRow> selectedRows = new List<DemoGridRow>();
                        foreach (var item in dgvDemos.SelectedItems)
                        {
                            selectedRows.Add((DemoGridRow)item);
                        }
                        
                        DataObject data = new DataObject("DemoGridRows", selectedRows);
                        DragDrop.DoDragDrop(dgvDemos, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void LstFolders_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("DemoGridRows"))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void LstFolders_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("DemoGridRows"))
            {
                var targetItem = e.Source as FrameworkElement;
                string targetFolder = null;
                
                // Traverse visual tree to find ListBoxItem
                DependencyObject parent = targetItem;
                while (parent != null && !(parent is ListBoxItem))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }
                
                if (parent is ListBoxItem)
                {
                    targetFolder = ((ListBoxItem)parent).DataContext as string;
                }
                
                if (string.IsNullOrEmpty(targetFolder))
                {
                    // If dropped directly on empty ListBox space, fallback to hovered item
                    ListBoxItem item = lstFolders.InputHitTest(e.GetPosition(lstFolders)) as ListBoxItem;
                    if (item != null) targetFolder = item.DataContext as string;
                }
                
                if (!string.IsNullOrEmpty(targetFolder))
                {
                    if (targetFolder == "[Все демки]")
                    {
                        ShowMessageDialog("Предупреждение", "Демки нельзя перетащить в общую категорию всех демок.", true);
                        return;
                    }
                    
                    var rows = e.Data.GetData("DemoGridRows") as List<DemoGridRow>;
                    if (rows != null && rows.Count > 0)
                    {
                        MoveSelectedDemos(rows, targetFolder);
                    }
                }
            }
        }

        private void MoveSelectedDemos(List<DemoGridRow> rows, string targetFolder)
        {
            string baseDir = GetDemosBaseDir();
            if (string.IsNullOrEmpty(baseDir)) return;

            string targetPath = Path.Combine(baseDir, targetFolder);
            if (!Directory.Exists(targetPath)) return;

            int movedCount = 0;
            foreach (var row in rows)
            {
                string oldPath = row.FilePath;
                string newPath = Path.Combine(targetPath, Path.GetFileName(oldPath));
                
                if (oldPath.Equals(newPath, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    if (File.Exists(newPath)) File.Delete(newPath);
                    File.Move(oldPath, newPath);

                    // Update metadata file
                    string oldRel = oldPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    string newRel = newPath.Substring(baseDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                    
                    DemoMetadata dm;
                    if (metadataDb.TryGetValue(oldRel, out dm))
                    {
                        metadataDb.Remove(oldRel);
                        DemoProcessor.SaveMetadataForDemo(baseDir, metadataDb, newRel, dm);
                    }
                    movedCount++;
                }
                catch (Exception ex)
                {
                    ShowMessageDialog("Ошибка", string.Format("Не удалось перенести демо {0}: {1}", Path.GetFileName(oldPath), ex.Message), true);
                }
            }

            if (movedCount > 0)
            {
                RefreshDemoList();
                lblStatus.Text = string.Format("Перемещено демок: {0}", movedCount);
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
            row.Map = GetMapEmoji(dm.Map) + " " + dm.Map;
            row.Score = dm.Score;
            row.Date = dm.Date;
            row.Note = dm.Note;

            // Re-calculate formatted fields
            bool isWin = false;
            string scoreText = dm.Score;
            if (!string.IsNullOrEmpty(dm.Score))
            {
                string[] parts = dm.Score.Split('-');
                if (parts.Length == 2)
                {
                    int s1, s2;
                    if (int.TryParse(parts[0].Trim(), out s1) && int.TryParse(parts[1].Trim(), out s2))
                    {
                        if (s1 > s2) { isWin = true; scoreText = "W " + s1 + " : " + s2; }
                        else { isWin = false; scoreText = "L " + s1 + " : " + s2; }
                    }
                }
            }
            row.IsWin = isWin;
            row.ScoreText = scoreText;

            string kdaText = "-";
            string kdRatioText = "-";
            string kdStatusText = "Normal";
            string adrText = "-";
            if (!string.IsNullOrEmpty(dm.KD) && dm.KD != "-")
            {
                Match mRatio = Regex.Match(dm.KD, @"^([\d.]+)");
                if (mRatio.Success)
                {
                    kdRatioText = mRatio.Groups[1].Value;
                    double ratioVal;
                    if (double.TryParse(kdRatioText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ratioVal))
                    {
                        kdStatusText = ratioVal >= 1.0 ? "High" : "Low";
                    }
                }
                Match mKda = Regex.Match(dm.KD, @"\(([^()]+)\)");
                if (mKda.Success)
                {
                    string kdaRaw = mKda.Groups[1].Value;
                    string[] kparts = kdaRaw.Split('/');
                    if (kparts.Length == 3) kdaText = string.Format("{0} / {1} / {2}", kparts[0], kparts[1], kparts[2]);
                    else if (kparts.Length == 2) kdaText = string.Format("{0} / {1} / -", kparts[0], kparts[1]);
                }
                Match mAdrVal = Regex.Match(dm.KD, @"\[([^[\]]+)\]");
                if (mAdrVal.Success) adrText = mAdrVal.Groups[1].Value;
            }
            row.KDA = kdaText;
            row.KDRatio = kdRatioText;
            row.KDStatus = kdStatusText;
            row.ADR = adrText;

            string dateTextFormatted = dm.Date;
            DateTime dtVal;
            if (DateTime.TryParse(dm.Date, out dtVal))
            {
                dateTextFormatted = dtVal.ToString("ddd d MMM", new System.Globalization.CultureInfo("ru-RU")) + "\n" + dtVal.ToString("HH:mm");
            }
            row.DateFormatted = dateTextFormatted;

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
            if (string.IsNullOrEmpty(cs2Dir)) return "";
            return Path.Combine(cs2Dir, "faceit_demos");
        }
    }
}
