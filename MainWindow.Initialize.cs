using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace FaceitDemoManager
{
    public partial class MainWindow : Window
    {
        private Microsoft.Web.WebView2.Wpf.WebView2 webView;
        private WebBridge webBridge;

        private async void InitializeWindow()
        {
            this.Title = "FACEIT Demo Hub";
            this.Width = 1050;
            this.Height = 700;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Opacity = 0;
            this.Loaded += MainWindow_Loaded;

            try
            {
                this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/FaceitDemoManager;component/app.ico", UriKind.RelativeOrAbsolute));
            }
            catch (Exception)
            {
                // Fallback
            }

            // WindowChrome configuration to enable native resize borders and drop shadows
            System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(6),
                UseAeroCaptionButtons = false,
                CornerRadius = new CornerRadius(12)
            });

            Border container = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = (Brush)new BrushConverter().ConvertFromString("#0e0e11"),
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#272730"),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };

            webView = new Microsoft.Web.WebView2.Wpf.WebView2();
            
            // Register native WPF Drag & Drop handlers on WebView2 control to bypass chromium sandbox blocks on Drag & Drop file path retrieval
            webView.AllowDrop = true;
            webView.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
                e.Handled = true;
            };
            webView.Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    ProcessManualFiles(files);
                }
            };

            container.Child = webView;
            this.Content = container;

            // Event Bindings
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.25)));
            this.BeginAnimation(Window.OpacityProperty, anim);

            string wwwrootPath = GetValidWwwrootPath();

            try
            {
                await webView.EnsureCoreWebView2Async();
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "app.assets",
                    wwwrootPath,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                webBridge = new WebBridge(this, webView.CoreWebView2);
                webView.Source = new Uri("https://app.assets/index.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка инициализации WebView2: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetValidWwwrootPath()
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            if (File.Exists(Path.Combine(localPath, "index.html")))
            {
                return localPath;
            }

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FaceitDemoHub", "wwwroot");
            ExtractEmbeddedResources(appDataPath);
            return appDataPath;
        }

        private void ExtractEmbeddedResources(string targetDir)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                foreach (string resourceName in assembly.GetManifestResourceNames())
                {
                    if (resourceName.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
                    {
                        string relPath = resourceName.Substring("wwwroot".Length).TrimStart('/', '\\', '.');
                        // Fix dot extension replacement if needed
                        if (resourceName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) relPath = "index.html";
                        else if (resourceName.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) relPath = "index.css";
                        else if (resourceName.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) relPath = "app.js";

                        string destFile = Path.Combine(targetDir, relPath);
                        string destFolder = Path.GetDirectoryName(destFile);
                        if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                using (FileStream fs = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                                {
                                    stream.CopyTo(fs);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
