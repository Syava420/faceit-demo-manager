using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
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
            if (e.Data.GetDataPresent("DemoGridRows") || e.Data.GetDataPresent("FolderItemPath"))
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
            var targetItem = e.Source as FrameworkElement;
            FolderItem targetFolder = null;
            
            // Traverse visual tree to find ListBoxItem
            DependencyObject parent = targetItem;
            while (parent != null && !(parent is ListBoxItem))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            if (parent is ListBoxItem)
            {
                targetFolder = ((ListBoxItem)parent).DataContext as FolderItem;
            }
            
            if (targetFolder == null)
            {
                // Fallback to hit-testing at the drop position, walking up to ListBoxItem container
                DependencyObject hit = lstFolders.InputHitTest(e.GetPosition(lstFolders)) as DependencyObject;
                while (hit != null && !(hit is ListBoxItem))
                {
                    hit = VisualTreeHelper.GetParent(hit);
                }
                if (hit is ListBoxItem listBoxItem)
                {
                    targetFolder = listBoxItem.DataContext as FolderItem;
                }
            }

            if (e.Data.GetDataPresent("DemoGridRows"))
            {
                if (targetFolder != null)
                {
                    string targetFolderRelPath = targetFolder.RelativePath;
                    if (targetFolderRelPath == "[Все демки]")
                    {
                        ShowMessageDialog("Предупреждение", "Демки нельзя перетащить в общую категорию всех демок.", true);
                        return;
                    }
                    
                    var rows = e.Data.GetData("DemoGridRows") as List<DemoGridRow>;
                    if (rows != null && rows.Count > 0)
                    {
                        MoveSelectedDemos(rows, targetFolderRelPath);
                    }
                }
            }
            else if (e.Data.GetDataPresent("FolderItemPath"))
            {
                var draggedPath = e.Data.GetData("FolderItemPath") as string;
                if (draggedPath != null)
                {
                    FolderItem draggedFolder = null;
                    foreach (var item in lstFolders.Items)
                    {
                        if (item is FolderItem fi && fi.RelativePath == draggedPath)
                        {
                            draggedFolder = fi;
                            break;
                        }
                    }
                    if (draggedFolder != null)
                    {
                        string targetRelPath = (targetFolder == null) ? "[Все демки]" : targetFolder.RelativePath;
                        MoveFolder(draggedFolder, targetRelPath);
                    }
                }
            }
        }

        private Point folderDragStartPoint;
        private void LstFolders_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            folderDragStartPoint = e.GetPosition(null);
        }

        private void LstFolders_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - folderDragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - folderDragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    FolderItem draggedFolder = null;

                    // 1. Try to find the item under the mouse dynamically
                    DependencyObject k = e.OriginalSource as DependencyObject;
                    while (k != null && !(k is ListBoxItem))
                    {
                        k = VisualTreeHelper.GetParent(k);
                    }

                    if (k is ListBoxItem listBoxItem && listBoxItem.DataContext is FolderItem fi)
                    {
                        draggedFolder = fi;
                    }

                    // 2. Fallback to selected item if tree walk failed
                    if (draggedFolder == null)
                    {
                        draggedFolder = lstFolders.SelectedItem as FolderItem;
                    }

                    if (draggedFolder != null)
                    {
                        if (draggedFolder.RelativePath == "[Все демки]" || draggedFolder.RelativePath == "General")
                            return;

                        DataObject data = new DataObject("FolderItemPath", draggedFolder.RelativePath);
                        DragDrop.DoDragDrop(lstFolders, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void MoveFolder(FolderItem src, string dest)
        {
            if (src.RelativePath == dest) return;

            // Prevent circular moving
            if (dest.StartsWith(src.RelativePath + "/", StringComparison.OrdinalIgnoreCase) || dest.Equals(src.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                ShowMessageDialog("Предупреждение", "Нельзя переместить папку саму в себя или в свои подпапки.", true);
                return;
            }

            string baseDir = GetDemosBaseDir();
            if (string.IsNullOrEmpty(baseDir)) return;

            string srcPath = Path.Combine(baseDir, src.RelativePath);
            string folderName = Path.GetFileName(srcPath);
            string destPath = (dest == "[Все демки]" || string.IsNullOrEmpty(dest)) ? Path.Combine(baseDir, folderName) : Path.Combine(baseDir, dest, folderName);

            if (string.Equals(Path.GetFullPath(srcPath), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Directory.Exists(destPath))
            {
                ShowMessageDialog("Ошибка", "Папка с таким именем уже существует в месте назначения.", true);
                return;
            }

            try
            {
                // Physically move directory
                Directory.Move(srcPath, destPath);

                // Update metadata relative paths
                string oldRelPrefix = src.RelativePath + "/";
                string newRelPrefix = (dest == "[Все демки]" || string.IsNullOrEmpty(dest)) ? folderName + "/" : dest + "/" + folderName + "/";

                List<string> keysToUpdate = new List<string>();
                foreach (string key in metadataDb.Keys)
                {
                    if (key.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        keysToUpdate.Add(key);
                    }
                }

                foreach (string oldKey in keysToUpdate)
                {
                    DemoMetadata dm = metadataDb[oldKey];
                    metadataDb.Remove(oldKey);
                    string newKey = newRelPrefix + oldKey.Substring(oldRelPrefix.Length);
                    metadataDb[newKey] = dm;
                }

                SaveEntireMetadataDb(baseDir);

                // Update nickname settings if applicable
                if (settings != null && settings.FolderNicknames != null)
                {
                    Dictionary<string, string> updatedNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    
                    // Copy all unrelated nicknames
                    foreach (var kvp in settings.FolderNicknames)
                    {
                        if (!kvp.Key.Equals(src.RelativePath, StringComparison.OrdinalIgnoreCase) && 
                            !kvp.Key.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            updatedNicknames[kvp.Key] = kvp.Value;
                        }
                    }

                    // Rename matching ones
                    string oldPathSelf = src.RelativePath;
                    string newPathSelf = (dest == "[Все демки]" || string.IsNullOrEmpty(dest)) ? folderName : dest + "/" + folderName;
                    
                    string nick;
                    if (settings.FolderNicknames.TryGetValue(oldPathSelf, out nick))
                    {
                        updatedNicknames[newPathSelf] = nick;
                    }

                    foreach (var kvp in settings.FolderNicknames)
                    {
                        if (kvp.Key.StartsWith(oldRelPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string newSubKey = newRelPrefix + kvp.Key.Substring(oldRelPrefix.Length);
                            newSubKey = newSubKey.TrimEnd('/');
                            updatedNicknames[newSubKey] = kvp.Value;
                        }
                    }

                    settings.FolderNicknames = updatedNicknames;
                    ConfigManager.Save(configPath, settings);
                }

                RefreshFolders();
                UpdateImportFolderCombobox();

                // Select the moved folder
                string finalDestPath = (dest == "[Все демки]" || string.IsNullOrEmpty(dest)) ? folderName : dest + "/" + folderName;
                foreach (var item in lstFolders.Items)
                {
                    if (item is FolderItem fi && fi.RelativePath == finalDestPath)
                    {
                        lstFolders.SelectedItem = fi;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog("Ошибка", "Не удалось переместить папку: " + ex.Message, true);
            }
        }

        private void SaveEntireMetadataDb(string baseDir)
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
    }
}
