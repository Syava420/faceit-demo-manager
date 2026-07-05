using System;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;

namespace FaceitDemoManager
{
    public class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        public static void Main()
        {
            try
            {
                // Включаем нативную четкость (DPI awareness) для исправления размытости шрифтов
                try
                {
                    SetProcessDPIAware();
                }
                catch { }

                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | (System.Net.SecurityProtocolType)12288 | System.Net.SecurityProtocolType.Tls11;
                App app = new App();
                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                File.WriteAllText(path, ex.ToString());
                MessageBox.Show("Ошибка при запуске: " + ex.Message + "\nПодробности записаны в crash_log.txt", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
