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
        private ComboBox CboImportMode;
        private ComboBox CboImportFolder;
        private Button btnBrowseDownloads;
        private Button btnAutoCS2;
        private Button btnBrowseCS2;
        private Button btnProcess;

        // Tab 2: Library Controls
        private Grid gridLibraryTab;
        private ListBox lstFolders;
        private TextBox txtSearch;
        private DataGrid dgvDemos;
        
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

        // Footer / Progress
        private TextBlock lblStatus;
        private ProgressBar prgBar;

        // App state
        private string configPath;
        private AppSettings settings;
        private FileSystemWatcher dlWatcher;
        private System.Windows.Forms.NotifyIcon trayIcon;
        private Dictionary<string, DemoMetadata> metadataDb = new Dictionary<string, DemoMetadata>(StringComparer.OrdinalIgnoreCase);
        private bool isUpdatingFields = false;
        private int userLevel = 0;
        private int userElo = 0;

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

        private void InitializeWindow()
        {
            this.Title = "FACEIT Demo Hub";
            this.Width = 1000;
            this.Height = 650;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Border root = (Border)XamlReader.Parse(GetXamlString());
            this.Content = root;

            // Resolve controls from XAML
            tabImport = (Button)root.FindName("TabImport");
            tabLibrary = (Button)root.FindName("TabLibrary");
            tabBinds = (Button)root.FindName("TabBinds");
            sidebarLibraryControls = (Grid)root.FindName("SidebarLibraryControls");
            txtSidebarNick = (TextBox)root.FindName("TxtSidebarNick");
            lblSidebarNick = (TextBlock)root.FindName("LblSidebarNick");

            gridImportTab = (Grid)root.FindName("GridImportTab");
            dragDropZone = (Border)root.FindName("DragDropZone");
            txtLogConsole = (TextBox)root.FindName("TxtLogConsole");
            txtDownloads = (TextBox)root.FindName("TxtDownloads");
            txtCS2 = (TextBox)root.FindName("TxtCS2");
            txtNickname = (TextBox)root.FindName("TxtNickname");
            ChkWatchFolder = (CheckBox)root.FindName("ChkWatchFolder");
            ChkTray = (CheckBox)root.FindName("ChkTray");
            chkVoiceInDemos = (CheckBox)root.FindName("ChkVoiceInDemos");
            CboImportMode = (ComboBox)root.FindName("CboImportMode");
            CboImportFolder = (ComboBox)root.FindName("CboImportFolder");
            btnBrowseDownloads = (Button)root.FindName("BtnBrowseDownloads");
            btnAutoCS2 = (Button)root.FindName("BtnAutoCS2");
            btnBrowseCS2 = (Button)root.FindName("BtnBrowseCS2");
            btnProcess = (Button)root.FindName("BtnProcess");

            gridLibraryTab = (Grid)root.FindName("GridLibraryTab");
            lstFolders = (ListBox)root.FindName("LstFolders");
            txtSearch = (TextBox)root.FindName("TxtSearch");
            dgvDemos = (DataGrid)root.FindName("DgvDemos");

            txtEditMap = (TextBox)root.FindName("TxtEditMap");
            txtEditScore = (TextBox)root.FindName("TxtEditScore");
            txtEditKD = (TextBox)root.FindName("TxtEditKD");
            txtEditDate = (TextBox)root.FindName("TxtEditDate");
            txtNoteEdit = (TextBox)root.FindName("TxtNoteEdit");

            btnNewCategory = (Button)root.FindName("BtnNewCategory");
            btnDeleteCategory = (Button)root.FindName("BtnDeleteCategory");
            btnPlay = (Button)root.FindName("BtnPlay");
            btnMoveDemo = (Button)root.FindName("BtnMoveDemo");
            btnDeleteDemo = (Button)root.FindName("BtnDeleteDemo");

            gridBindsTab = (Grid)root.FindName("GridBindsTab");
            dgvBinds = (DataGrid)root.FindName("DgvBinds");
            chkAutoApplyBinds = (CheckBox)root.FindName("ChkAutoApplyBinds");
            btnResetBinds = (Button)root.FindName("BtnResetBinds");

            lblStatus = (TextBlock)root.FindName("LblStatus");
            prgBar = (ProgressBar)root.FindName("PrgBar");

            // Event Bindings
            this.Closing += MainWindow_Closing;
            
            // TitleBar Drag
            Grid titleBar = (Grid)root.FindName("TitleBar");
            titleBar.MouseLeftButtonDown += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
            };

            Button btnMin = (Button)root.FindName("BtnMinimize");
            Button btnClose = (Button)root.FindName("BtnClose");
            btnMin.Click += (s, e) => this.WindowState = WindowState.Minimized;
            btnClose.Click += (s, e) => this.Close();

            // Tab Switching
            tabImport.Click += (s, e) => SwitchTab(0);
            tabLibrary.Click += (s, e) => SwitchTab(1);
            tabBinds.Click += (s, e) => SwitchTab(2);

            // Browse Buttons
            btnBrowseDownloads.Click += (s, e) => BrowseFolder("Выберите папку загрузок", txtDownloads);
            
            // CS2 Browse and Auto-detect
            btnBrowseCS2.Click += (s, e) => BrowseFolder("Выберите папку CS2 (game\\csgo)", txtCS2);
            btnAutoCS2.Click += (s, e) => {
                string detected = ConfigManager.AutoDetectCS2Path();
                if (!string.IsNullOrEmpty(detected))
                {
                    txtCS2.Text = detected;
                    SaveConfig();
                    ShowMessageDialog("Авто-поиск", "Папка CS2 успешно обнаружена:\n" + detected);
                }
                else
                {
                    ShowMessageDialog("Авто-поиск", "Не удалось автоматически найти папку CS2. Пожалуйста, укажите её вручную с помощью кнопки '...'.", true);
                }
            };

            // Drag and Drop
            dragDropZone.Drop += MainWindow_Drop;
            dragDropZone.DragOver += (s, e) => e.Effects = DragDropEffects.Copy;
            dragDropZone.MouseLeftButtonDown += DragDropZone_MouseLeftButtonDown;

            // Scan / Watcher triggers
            btnProcess.Click += BtnProcess_Click;
            ChkWatchFolder.Checked += (s, e) => ToggleWatcherSetting(true);
            ChkWatchFolder.Unchecked += (s, e) => ToggleWatcherSetting(false);
            
            // Voice Setting triggers
            chkVoiceInDemos.Checked += (s, e) => SaveConfig();
            chkVoiceInDemos.Unchecked += (s, e) => SaveConfig();

            // Auto-apply binds triggers
            chkAutoApplyBinds.Checked += (s, e) => SaveConfig();
            chkAutoApplyBinds.Unchecked += (s, e) => SaveConfig();

            // Reset binds button
            btnResetBinds.Click += BtnResetBinds_Click;

            // Library search & list selection
            txtSearch.TextChanged += (s, e) => RefreshDemoList();
            
            // Setup folder selection text updates contextually
            lstFolders.SelectionChanged += (s, e) => {
                UpdateNicknameInput();
                RefreshDemoList();
            };

            // Nickname text edits
            txtNickname.TextChanged += (s, e) => {
                if (isUpdatingNickname || settings == null) return;
                settings.Nickname = txtNickname.Text.Trim();
                ConfigManager.Save(configPath, settings);
            };
            if (txtSidebarNick != null)
            {
                txtSidebarNick.TextChanged += (s, e) => {
                    if (isUpdatingNickname || settings == null) return;
                    string selectedFolder = lstFolders != null && lstFolders.SelectedItem != null ? lstFolders.SelectedItem.ToString() : null;
                    if (!string.IsNullOrEmpty(selectedFolder) && selectedFolder != "[Все демки]")
                    {
                        if (settings.FolderNicknames == null)
                        {
                            settings.FolderNicknames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                        settings.FolderNicknames[selectedFolder] = txtSidebarNick.Text.Trim();
                        ConfigManager.Save(configPath, settings);
                    }
                };
            }

            btnNewCategory.Click += BtnNewCategory_Click;
            btnDeleteCategory.Click += BtnDeleteCategory_Click;
            btnPlay.Click += BtnPlay_Click;
            btnMoveDemo.Click += BtnMoveDemo_Click;
            btnDeleteDemo.Click += BtnDeleteDemo_Click;

            dgvDemos.SelectionChanged += DgvDemos_SelectionChanged;
            txtEditMap.TextChanged += EditField_TextChanged;
            txtEditScore.TextChanged += EditField_TextChanged;
            txtEditKD.TextChanged += EditField_TextChanged;
            txtEditDate.TextChanged += EditField_TextChanged;
            txtNoteEdit.TextChanged += EditField_TextChanged;

            CboImportMode.SelectionChanged += CboImportMode_SelectionChanged;

            // Setup double-click in DataGrid to play demo
            dgvDemos.MouseDoubleClick += DgvDemos_MouseDoubleClick;

            // Setup Drag & Drop from DataGrid to side Folders
            dgvDemos.PreviewMouseLeftButtonDown += DgvDemos_PreviewMouseLeftButtonDown;
            dgvDemos.MouseMove += DgvDemos_MouseMove;
            lstFolders.DragOver += LstFolders_DragOver;
            lstFolders.Drop += LstFolders_Drop;
        }

        private void SwitchTab(int tabIndex)
        {
            // Reset backgrounds & foregrounds
            tabImport.Background = Brushes.Transparent;
            tabImport.Foreground = Brushes.Gray;
            tabLibrary.Background = Brushes.Transparent;
            tabLibrary.Foreground = Brushes.Gray;
            tabBinds.Background = Brushes.Transparent;
            tabBinds.Foreground = Brushes.Gray;

            gridImportTab.Visibility = Visibility.Collapsed;
            gridLibraryTab.Visibility = Visibility.Collapsed;
            gridBindsTab.Visibility = Visibility.Collapsed;
            sidebarLibraryControls.Visibility = Visibility.Collapsed;

            var root = (Border)this.Content;
            var catHeader = (TextBlock)root.FindName("SidebarCategoriesHeader");
            if (catHeader != null) catHeader.Visibility = Visibility.Collapsed;

            if (tabIndex == 0) // Import
            {
                tabImport.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6"));
                tabImport.Foreground = Brushes.White;
                gridImportTab.Visibility = Visibility.Visible;
            }
            else if (tabIndex == 1) // Library
            {
                tabLibrary.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6"));
                tabLibrary.Foreground = Brushes.White;
                gridLibraryTab.Visibility = Visibility.Visible;
                sidebarLibraryControls.Visibility = Visibility.Visible;
                if (catHeader != null) catHeader.Visibility = Visibility.Visible;
                RefreshFolders();
            }
            else if (tabIndex == 2) // Binds
            {
                tabBinds.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6"));
                tabBinds.Foreground = Brushes.White;
                gridBindsTab.Visibility = Visibility.Visible;
                
                // Refresh binds list to reflect updates
                dgvBinds.ItemsSource = null;
                dgvBinds.ItemsSource = settings.Binds;
            }
        }

        private void AppendLog(string message)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                txtLogConsole.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
                txtLogConsole.ScrollToEnd();
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
                }
            }
            
            isUpdatingNickname = false;
        }

        private void FetchUserEloAsync()
        {
            string nickname = "";
            this.Dispatcher.Invoke(new Action(() => {
                nickname = txtNickname.Text.Trim();
            }));

            if (string.IsNullOrEmpty(nickname))
            {
                userLevel = 0;
                userElo = 0;
                return;
            }

            Thread thread = new Thread(() =>
            {
                int lvl = 0;
                int elo = 0;
                try
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) FaceitDemoHub");
                        string json = wc.DownloadString("https://api.faceit.com/users/v1/nicknames/" + nickname);
                        Match mLevel = Regex.Match(json, @"""skill_level""\s*:\s*(\d+)");
                        Match mElo = Regex.Match(json, @"""faceit_elo""\s*:\s*(\d+)");
                        if (mLevel.Success) lvl = int.Parse(mLevel.Groups[1].Value);
                        if (mElo.Success) elo = int.Parse(mElo.Groups[1].Value);
                    }
                }
                catch { }

                this.userLevel = lvl;
                this.userElo = elo;

                this.Dispatcher.Invoke(new Action(() => {
                    RefreshDemoList();
                }));
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void LoadConfig()
        {
            settings = ConfigManager.Load(configPath);

            txtDownloads.Text = settings.DownloadsPath;
            txtCS2.Text = settings.CS2Path;
            if (txtNickname != null) txtNickname.Text = settings.Nickname;
            
            UpdateNicknameInput();
            
            ChkWatchFolder.IsChecked = settings.WatchFolder;
            ChkTray.IsChecked = settings.MinimizeTray;
            chkVoiceInDemos.IsChecked = settings.EnableDemoVoice;
            chkAutoApplyBinds.IsChecked = settings.AutoApplyBinds;

            dgvBinds.ItemsSource = settings.Binds;

            // Set Import Mode ComboBox selection
            foreach (ComboBoxItem item in CboImportMode.Items)
            {
                if (item.Tag.ToString().Equals(settings.ImportMode, StringComparison.OrdinalIgnoreCase))
                {
                    CboImportMode.SelectedItem = item;
                    break;
                }
            }

            // Set Target Import Folder
            UpdateImportFolderCombobox();
            if (CboImportFolder.Items.Contains(settings.TargetImportFolder))
            {
                CboImportFolder.SelectedItem = settings.TargetImportFolder;
            }
        }

        private void SaveConfig()
        {
            if (settings == null) settings = new AppSettings();

            settings.DownloadsPath = txtDownloads.Text.Trim();
            settings.CS2Path = txtCS2.Text.Trim();
            settings.Nickname = txtNickname.Text.Trim();
            
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

            settings.WatchFolder = ChkWatchFolder.IsChecked == true;
            settings.MinimizeTray = ChkTray.IsChecked == true;
            settings.EnableDemoVoice = chkVoiceInDemos.IsChecked == true;
            settings.AutoApplyBinds = chkAutoApplyBinds.IsChecked == true;

            var selectedItem = (ComboBoxItem)CboImportMode.SelectedItem;
            if (selectedItem != null) settings.ImportMode = selectedItem.Tag.ToString();
            
            settings.TargetImportFolder = CboImportFolder.SelectedItem != null ? CboImportFolder.SelectedItem.ToString() : "General";

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
            SaveConfig();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            SaveConfig();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
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
