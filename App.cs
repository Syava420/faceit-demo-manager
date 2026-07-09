using System;
using System.Windows;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FaceitDemoManager
{
    public class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        public static void Main()
        {
            // Регистрируем глобальные перехватчики исключений до запуска UI
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

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
                app.DispatcherUnhandledException += App_DispatcherUnhandledException;
                app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                HandleFatalException(ex);
            }
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Предотвращаем падение приложения по умолчанию
            e.Handled = true;
            LogAndShowException(e.Exception, isRecoverable: true);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception ?? new Exception("Неизвестное исключение AppDomain");
            LogAndShowException(ex, isRecoverable: false);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            LogAndShowException(e.Exception, isRecoverable: true);
        }

        private static void LogAndShowException(Exception ex, bool isRecoverable)
        {
            try
            {
                // Записываем ошибку в crash_log.txt
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                File.WriteAllText(path, $"[{DateTime.Now}] Recoverable={isRecoverable}\n" + ex.ToString());
            }
            catch { }

            // Показываем окно ошибки в UI-потоке
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    ShowErrorDialog(ex, isRecoverable);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => ShowErrorDialog(ex, isRecoverable));
                }
            }
            else
            {
                MessageBox.Show(ex.Message, "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ShowErrorDialog(Exception ex, bool isRecoverable)
        {
            try
            {
                var errWin = new ErrorHandlerWindow(ex, isRecoverable);
                errWin.ShowDialog();
            }
            catch (Exception uiEx)
            {
                // Резервный вариант, если само окно ошибки упало при отрисовке
                MessageBox.Show($"Ошибка: {ex.Message}\n\nНе удалось запустить диалоговое окно ошибок: {uiEx.Message}", 
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void HandleFatalException(Exception ex)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
            try
            {
                File.WriteAllText(path, ex.ToString());
            }
            catch { }

            MessageBox.Show("Ошибка при запуске: " + ex.Message + "\nПодробности записаны в crash_log.txt", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
