using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Alm.Authentication;
using System.Diagnostics;

namespace GitHub.Authentication
{
    public static class Authcode
    {
        public static bool ModalPrompt(TargetUri targetUri, GithubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            Trace.WriteLine("Program::GithubAuthcodeModalPrompt");

            var twoFactorViewModel = new ViewModels.TwoFactorViewModel(resultType == GithubAuthenticationResultType.TwoFactorSms);

            Trace.WriteLine("   prompting user for authentication code.");

            StartSTATask(() =>
            {
                if (!UriParser.IsKnownScheme("pack"))
                {
                    UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);
                }
                var app = new Application();
                var appResources = new Uri("pack://application:,,,/GitHub.Authentication;component/AppResources.xaml", UriKind.RelativeOrAbsolute);
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = appResources });
                app.Run(new GitHub_Authentication.TwoFactorWindow { ViewModel = twoFactorViewModel });
            })
            .Wait();

            // If the user cancels the dialog, we need to ignore anything they've
            // typed into the authentication code.
            bool authenticationCodeValid = twoFactorViewModel.Result == ViewModels.TwoFactorResult.Ok
                                        && twoFactorViewModel.IsValid;

            authenticationCode = authenticationCodeValid
                ? twoFactorViewModel.AuthenticationCode
                : null;

            return authenticationCodeValid;
        }

        static Task StartSTATask(Action action)
        {
            var completionSource = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                    completionSource.SetResult(null);
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completionSource.Task;
        }
    }
}
