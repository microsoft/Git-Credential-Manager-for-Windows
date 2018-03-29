using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GcmParentWindowTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            Uri uri = new Uri((string)button.Tag);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            InvokeGcm(uri, hwnd);
        }

        private void BogusHwnd_Click(object sender, RoutedEventArgs e)
        {
            InvokeGcm(new Uri("https://github.com"), new IntPtr(unchecked((int)0xDEADBEEF)));
        }

        private void InvokeGcm(Uri uri, IntPtr hwnd)
        {
            Tuple<string, string> output = Gcm.Invoke(uri, true, hwnd);
            StdOutTextBlock.Text = output.Item1;
            StdErrTextBlock.Text = output.Item2;
        }
    }

    public static class Gcm
    {
        public static Tuple<string, string> Invoke(Uri uri, bool interactive, IntPtr hwnd = default(IntPtr))
        {
            const string gcmExePath = "git-credential-manager.exe";

            string arguments = "get";
            if (hwnd != IntPtr.Zero)
            {
                arguments += $" --ownerHwnd {hwnd.ToString()}";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                 FileName = gcmExePath,
                 Arguments = arguments,
                 CreateNoWindow = true,
                 UseShellExecute = false,
                 RedirectStandardError = true,
                 RedirectStandardInput = true,
                 RedirectStandardOutput = true,
                 StandardOutputEncoding = Encoding.UTF8,
            };

            startInfo.Environment.Add(new KeyValuePair<string, string>("GCM_INTERACTIVE", interactive.ToString()));

            Process process = Process.Start(startInfo);

            process.StandardInput.WriteLine($"protocol={uri.Scheme}");
            process.StandardInput.WriteLine($"host={uri.DnsSafeHost}");
            process.StandardInput.WriteLine();

            process.WaitForExit();

            return new Tuple<string, string>(process.StandardOutput.ReadToEnd(), process.StandardError.ReadToEnd());
        }
    }
}
