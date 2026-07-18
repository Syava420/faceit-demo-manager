using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        // Navigation Buttons
        private Button tabImport;
        private Button tabLibrary;
        private Button tabBinds;
        private Grid sidebarLibraryControls;
        private TextBox txtSidebarNick;
        private TextBlock lblSidebarNick;

        // Tab 1: Import Controls
        private Grid gridImportTab;
        private Border dragDropZone;
        private TextBox txtLogConsole;
        private TextBox txtDownloads;
        private TextBox txtCS2;
        private TextBox txtNickname;
        private CheckBox ChkWatchFolder;
        private CheckBox ChkTray;
        private CheckBox chkVoiceInDemos;
        private CheckBox chkDeleteArchives;
        private ComboBox CboImportMode;
        private ComboBox CboImportFolder;
        private Button btnBrowseDownloads;
        private Button btnAutoCS2;
        private Button btnBrowseCS2;
        private Button btnProcess;
        private Button btnClearLog;

        // Tab 2: Library Controls
        private Grid gridLibraryTab;
        private ListBox lstFolders;
        private TextBox txtSearch;
        private DataGrid dgvDemos;
        private WrapPanel pnlMapFilters;
        private string selectedMapFilter = null;
        private string selectedCategory = "General";
        
        // Match Editing Fields
        private TextBox txtEditMap;
        private TextBox txtEditScore;
        private TextBox txtEditKD;
        private TextBox txtEditDate;
        private TextBox txtNoteEdit;
        
        // Library Actions
        private Button btnNewCategory;
        private Button btnDeleteCategory;
        private Button btnPlay;
        private Button btnMoveDemo;
        private Button btnDeleteDemo;

        // Tab 3: Binds Controls
        private Grid gridBindsTab;
        private DataGrid dgvBinds;
        private CheckBox chkAutoApplyBinds;
        private Button btnResetBinds;
        private Button btnAddBind;
        private Button btnDeleteBind;

        // Footer / Progress
        private TextBlock lblStatus;
        private ProgressBar prgBar;

        // App state
        private string configPath;
        private AppSettings settings;
        private FileSystemWatcher dlWatcher;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private Dictionary<string, DemoMetadata> metadataDb = new Dictionary<string, DemoMetadata>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> collapsedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool isUpdatingFields = false;
        private int userLevel = 0;
        private int userElo = 0;
        private string lastSidebarNick = "";
        private string lastGlobalNick = "";

        public MainWindow()
        {
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            InitializeWindow();
            LoadConfig();
            RefreshFolders();
            UpdateImportFolderCombobox();
            InitializeWatcher();
            FetchUserEloAsync();
        }



        private void AnimateTabFadeIn(UIElement element)
        {
            if (element == null) return;
            element.Visibility = Visibility.Visible;
            var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.2)));
            element.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void SwitchTab(int tabIndex)
        {
            if (tabImport == null) return;
            // Reset backgrounds & foregrounds
            tabImport.Background = Brushes.Transparent;
            tabImport.Foreground = Brushes.Gray;
            tabLibrary.Background = Brushes.Transparent;
            tabLibrary.Foreground = Brushes.Gray;
            tabBinds.Background = Brushes.Transparent;
            tabBinds.Foreground = Brushes.Gray;

            // Collapse and reset animations
            gridImportTab.Visibility = Visibility.Collapsed;
            gridImportTab.BeginAnimation(UIElement.OpacityProperty, null);
            gridLibraryTab.Visibility = Visibility.Collapsed;
            gridLibraryTab.BeginAnimation(UIElement.OpacityProperty, null);
            gridBindsTab.Visibility = Visibility.Collapsed;
            gridBindsTab.BeginAnimation(UIElement.OpacityProperty, null);
            sidebarLibraryControls.Visibility = Visibility.Collapsed;
            sidebarLibraryControls.BeginAnimation(UIElement.OpacityProperty, null);

            var root = (Border)this.Content;
            var catHeader = (TextBlock)root.FindName("SidebarCategoriesHeader");
            if (catHeader != null)
            {
                catHeader.Visibility = Visibility.Collapsed;
                catHeader.BeginAnimation(UIElement.OpacityProperty, null);
            }

            if (tabIndex == 0) // Import
            {
                tabImport.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c3aed"));
                tabImport.Foreground = Brushes.White;
                AnimateTabFadeIn(gridImportTab);
            }
            else if (tabIndex == 1) // Library
            {
                tabLibrary.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c3aed"));
                tabLibrary.Foreground = Brushes.White;
                AnimateTabFadeIn(gridLibraryTab);
                AnimateTabFadeIn(sidebarLibraryControls);
                if (catHeader != null) AnimateTabFadeIn(catHeader);
                RefreshFolders();
            }
            else if (tabIndex == 2) // Binds
            {
                tabBinds.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c3aed"));
                tabBinds.Foreground = Brushes.White;
                AnimateTabFadeIn(gridBindsTab);
                
                // Refresh binds list to reflect updates
                dgvBinds.ItemsSource = null;
                dgvBinds.ItemsSource = settings.Binds;
            }
        }

        private void AppendLog(string message)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                if (txtLogConsole != null)
                {
                    txtLogConsole.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
                    txtLogConsole.ScrollToEnd();
                }
                if (webBridge != null)
                {
                    webBridge.SendLog(message);
                }
            }));
        }

        private void BrowseFolder(string title, TextBox tb)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = title;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tb.Text = dialog.SelectedPath;
                    RefreshFolders();
                    UpdateImportFolderCombobox();
                }
            }
        }

        private bool isUpdatingNickname = false;
        private void UpdateNicknameInput()
        {
            if (settings == null) return;
            
            isUpdatingNickname = true;
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            
            if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Все devки]" || selectedFolder == "[Все демки]")
            {
                if (lblSidebarNick != null) lblSidebarNick.Visibility = Visibility.Collapsed;
                if (txtSidebarNick != null)
                {
                    txtSidebarNick.Visibility = Visibility.Collapsed;
                    txtSidebarNick.Text = "";
                    lastSidebarNick = "";
                }
            }
            else
            {
                string folderNick = "";
                if (settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue(selectedFolder, out folderNick))
                {
                    // Found
                }
                else
                {
                    folderNick = "";
                }
                
                if (lblSidebarNick != null)
                {
                    lblSidebarNick.Visibility = Visibility.Visible;
                    lblSidebarNick.Text = string.Format("Никнейм для '{0}':", selectedFolder);
                }
                if (txtSidebarNick != null)
                {
                    txtSidebarNick.Visibility = Visibility.Visible;
                    txtSidebarNick.Text = folderNick;
                    lastSidebarNick = folderNick;
                }
            }
            
            isUpdatingNickname = false;
        }

        private async void FetchUserEloAsync()
        {
            string nickname = "";
            this.Dispatcher.Invoke(new Action(() => {
                nickname = txtNickname != null ? txtNickname.Text.Trim() : (settings != null ? settings.Nickname : "");
            }));

            if (string.IsNullOrEmpty(nickname))
            {
                userLevel = 0;
                userElo = 0;
                return;
            }

            var (lvl, elo) = await FaceitApiClient.FetchUserEloAsync(nickname);

            this.userLevel = lvl;
            this.userElo = elo;

            this.Dispatcher.Invoke(new Action(() => {
                RefreshDemoList();
            }));
        }

        private void LoadConfig()
        {
            settings = ConfigManager.Load(configPath);

            if (txtDownloads != null) txtDownloads.Text = settings.DownloadsPath;
            if (txtCS2 != null) txtCS2.Text = settings.CS2Path;
            
            if (ChkWatchFolder != null) ChkWatchFolder.IsChecked = settings.WatchFolder;
            if (ChkTray != null) ChkTray.IsChecked = settings.MinimizeTray;
            if (chkVoiceInDemos != null) chkVoiceInDemos.IsChecked = settings.EnableDemoVoice;
            if (chkAutoApplyBinds != null) chkAutoApplyBinds.IsChecked = settings.AutoApplyBinds;
            if (chkDeleteArchives != null) chkDeleteArchives.IsChecked = settings.DeleteArchivesAfterUnpack;

            if (dgvBinds != null) dgvBinds.ItemsSource = settings.Binds;

            // Set Import Mode ComboBox selection
            if (CboImportMode != null)
            {
                foreach (ComboBoxItem item in CboImportMode.Items)
                {
                    if (item.Tag.ToString().Equals(settings.ImportMode, StringComparison.OrdinalIgnoreCase))
                    {
                        CboImportMode.SelectedItem = item;
                        break;
                    }
                }
            }

            // Set Target Import Folder
            UpdateImportFolderCombobox();
            if (CboImportFolder != null && CboImportFolder.Items.Contains(settings.TargetImportFolder))
            {
                CboImportFolder.SelectedItem = settings.TargetImportFolder;
            }

            UpdateImportNicknameDisplay();
            UpdateNicknameInput();
        }

        private void SaveConfig()
        {
            if (settings == null) settings = new AppSettings();

            if (txtDownloads != null) settings.DownloadsPath = txtDownloads.Text.Trim();
            if (txtCS2 != null) settings.CS2Path = txtCS2.Text.Trim();
            
            // Save global nickname ONLY if we are not editing specific folders
            string importMode = CboImportMode != null && CboImportMode.SelectedItem != null ? ((ComboBoxItem)CboImportMode.SelectedItem).Tag.ToString() : "General";
            if (importMode == "Ask")
            {
                if (txtNickname != null) settings.Nickname = txtNickname.Text.Trim();
            }
            else if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (txtNickname != null) settings.FolderNicknames[targetFolder] = txtNickname.Text.Trim();
            }
            else if (importMode == "General")
            {
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (txtNickname != null) settings.FolderNicknames["General"] = txtNickname.Text.Trim();
            }
            
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (!string.IsNullOrEmpty(selectedFolder) && selectedFolder != "[Все демки]")
            {
                if (settings.FolderNicknames == null)
                {
                    settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                if (txtSidebarNick != null)
                {
                    settings.FolderNicknames[selectedFolder] = txtSidebarNick.Text.Trim();
                }
            }

            if (ChkWatchFolder != null) settings.WatchFolder = ChkWatchFolder.IsChecked == true;
            if (ChkTray != null) settings.MinimizeTray = ChkTray.IsChecked == true;
            if (chkVoiceInDemos != null) settings.EnableDemoVoice = chkVoiceInDemos.IsChecked == true;
            if (chkAutoApplyBinds != null) settings.AutoApplyBinds = chkAutoApplyBinds.IsChecked == true;
            if (chkDeleteArchives != null) settings.DeleteArchivesAfterUnpack = chkDeleteArchives.IsChecked == true;

            if (CboImportMode != null)
            {
                var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
                if (selectedItem != null) settings.ImportMode = selectedItem.Tag.ToString();
            }
            
            if (CboImportFolder != null)
            {
                settings.TargetImportFolder = CboImportFolder.SelectedItem != null ? CboImportFolder.SelectedItem.ToString() : "General";
            }

            ConfigManager.Save(configPath, settings);
        }

        private void CboImportMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboImportFolder == null) return;
            var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
            if (selectedItem != null)
            {
                string mode = selectedItem.Tag.ToString();
                CboImportFolder.IsEnabled = (mode == "Specific");
            }
            UpdateImportNicknameDisplay();
            SaveConfig();
        }

        private void CboImportFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateImportNicknameDisplay();
            SaveConfig();
        }

        private void UpdateImportNicknameDisplay()
        {
            if (CboImportMode == null || CboImportFolder == null || txtNickname == null || settings == null) return;

            isUpdatingNickname = true;

            var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
            string mode = selectedItem != null ? selectedItem.Tag.ToString() : "General";

            string currentNick = "";
            if (mode == "Specific" && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue(targetFolder, out currentNick))
                {
                    // Found
                }
                else
                {
                    currentNick = settings.Nickname;
                }
            }
            else if (mode == "General")
            {
                if (settings.FolderNicknames != null && settings.FolderNicknames.TryGetValue("General", out currentNick))
                {
                    // Found
                }
                else
                {
                    currentNick = settings.Nickname;
                }
            }
            else
            {
                currentNick = settings.Nickname;
            }

            txtNickname.Text = currentNick;
            lastGlobalNick = currentNick;

            isUpdatingNickname = false;
        }

        private void CommitSidebarNickname()
        {
            if (settings == null || isUpdatingNickname) return;
            string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
            if (string.IsNullOrEmpty(selectedFolder) || selectedFolder == "[Все демки]") return;

            string newNick = txtSidebarNick.Text.Trim();
            if (newNick == lastSidebarNick) return; // No change

            isUpdatingNickname = true;
            if (settings.FolderNicknames == null)
            {
                settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            settings.FolderNicknames[selectedFolder] = newNick;
            ConfigManager.Save(configPath, settings);
            
            // Sync settings textbox if the same folder is currently selected in the import tab
            string importMode = CboImportMode != null && CboImportMode.SelectedItem != null ? ((ComboBoxItem)CboImportMode.SelectedItem).Tag.ToString() : "General";
            if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (targetFolder.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase))
                {
                    if (txtNickname != null) txtNickname.Text = newNick;
                }
            }
            else if (importMode == "General" && selectedFolder.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                if (txtNickname != null) txtNickname.Text = newNick;
            }
            
            lastSidebarNick = newNick;
            isUpdatingNickname = false;

            // Trigger re-fetching stats for this folder
            UpdateFolderDemosStats(selectedFolder, newNick);
        }

        private void CommitGlobalNickname()
        {
            if (settings == null || isUpdatingNickname) return;
            string newNick = txtNickname.Text.Trim();
            if (newNick == lastGlobalNick) return; // No change

            isUpdatingNickname = true;
            string importMode = CboImportMode != null && CboImportMode.SelectedItem != null ? ((ComboBoxItem)CboImportMode.SelectedItem).Tag.ToString() : "General";
            
            if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                string targetFolder = CboImportFolder.SelectedItem.ToString();
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                settings.FolderNicknames[targetFolder] = newNick;
                
                // Sync sidebar if the same folder is selected there
                string selectedSidebarFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
                if (selectedSidebarFolder != null && selectedSidebarFolder.Equals(targetFolder, StringComparison.OrdinalIgnoreCase))
                {
                    if (txtSidebarNick != null) txtSidebarNick.Text = newNick;
                }
            }
            else if (importMode == "General")
            {
                if (settings.FolderNicknames == null) settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                settings.FolderNicknames["General"] = newNick;
                
                // Sync sidebar if General is selected
                string selectedSidebarFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
                if (selectedSidebarFolder != null && selectedSidebarFolder.Equals("General", StringComparison.OrdinalIgnoreCase))
                {
                    if (txtSidebarNick != null) txtSidebarNick.Text = newNick;
                }
            }
            else
            {
                settings.Nickname = newNick;
            }

            ConfigManager.Save(configPath, settings);
            lastGlobalNick = newNick;
            isUpdatingNickname = false;

            // If a specific folder's nickname was changed via the settings tab, update its demos stats too!
            if (importMode == "Specific" && CboImportFolder != null && CboImportFolder.SelectedItem != null)
            {
                UpdateFolderDemosStats(CboImportFolder.SelectedItem.ToString(), newNick);
            }
            else if (importMode == "General")
            {
                UpdateFolderDemosStats("General", newNick);
            }
        }

        private void UpdateFolderDemosStats(string folderName, string nickname)
        {
            if (string.IsNullOrEmpty(folderName) || folderName == "[Все демки]" || string.IsNullOrEmpty(nickname)) return;

            string baseDir = GetDemosBaseDir();
            System.Threading.Tasks.Task.Run(async () =>
            {
                await FaceitApiClient.UpdateFolderDemosStatsAsync(
                    folderName,
                    nickname,
                    baseDir,
                    this.metadataDb,
                    msg => AppendLog(msg),
                    (relPath, dm) => { },
                    () => {
                        this.Dispatcher.BeginInvoke(new Action(() => {
                            RefreshDemoList();
                        }));
                    }
                );
            });
        }

        private bool isClosingAnimationCompleted = false;
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!isClosingAnimationCompleted)
            {
                e.Cancel = true;
                SaveConfig();
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }

                var anim = new System.Windows.Media.Animation.DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.2)));
                anim.Completed += (s, ev) =>
                {
                    isClosingAnimationCompleted = true;
                    this.Close();
                };
                this.BeginAnimation(Window.OpacityProperty, anim);
            }
        }

        private void Log(string message, bool isError)
        {
            AppendLog((isError ? "Ошибка: " : "") + message);
        }

        private void UpdateProgress(int val, int max)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                prgBar.Maximum = max;
                prgBar.Value = val;
            }));
        }

        private void ResetProgress()
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                prgBar.Value = 0;
            }));
        }


    }
}
