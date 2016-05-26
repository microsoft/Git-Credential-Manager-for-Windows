using System;
using System.Diagnostics;
using System.Windows.Input;

namespace GitHub.Authentication.Helpers
{
    /// <summary>
    /// Command that opens a browser to the URL specified by the
    /// command parameter.
    /// </summary>
    public class HyperLinkCommand : ActionCommand
    {
        public HyperLinkCommand() : base(ExecuteNavigateUrl)
        {
        }

        static void ExecuteNavigateUrl(object parameter)
        {
            var commandParameter = parameter as string;
            if (string.IsNullOrWhiteSpace(commandParameter)) return;

            Uri navigateUrl;

            if (Uri.TryCreate(commandParameter, UriKind.Absolute, out navigateUrl))
            {
                Process.Start(new ProcessStartInfo(navigateUrl.AbsoluteUri));
            }
        }
    }
}
