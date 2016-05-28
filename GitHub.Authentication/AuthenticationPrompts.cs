using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitHub.Authentication.ViewModels;
using Microsoft.Alm.Authentication;

namespace GitHub.Authentication
{
    public static class AuthenticationPrompts
    {
        public static bool CredentialModalPrompt(TargetUri targetUri, out string username, out string password)
        {
            Trace.WriteLine("Program::GithubCredentialModalPrompt");

            var credentialViewModel = new CredentialsViewModel();

            ShowViewModel(credentialViewModel);

            // If the user cancels the dialog, we need to ignore anything they've
            // typed into the authentication code.
            bool credentialValid = credentialViewModel.Result == AuthenticationDialogResult.Ok
                                        && credentialViewModel.ModelValidator.IsValid;

            username = credentialViewModel.Login;
            password = credentialViewModel.Password;
            return credentialValid;
        }

        public static bool AuthenticationCodeModalPrompt(TargetUri targetUri, GithubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            Trace.WriteLine("Program::GithubAuthcodeModalPrompt");

            var twoFactorViewModel = new TwoFactorViewModel(resultType == GithubAuthenticationResultType.TwoFactorSms);

            Trace.WriteLine("   prompting user for authentication code.");

            ShowViewModel(twoFactorViewModel);

            // If the user cancels the dialog, we need to ignore anything they've
            // typed into the authentication code.
            bool authenticationCodeValid = twoFactorViewModel.Result == AuthenticationDialogResult.Ok
                                        && twoFactorViewModel.IsValid;

            authenticationCode = authenticationCodeValid
                ? twoFactorViewModel.AuthenticationCode
                : null;

            return authenticationCodeValid;
        }

        static void ShowViewModel(ViewModel viewModel)
        {
            StartSTATask(() =>
            {
                if (!UriParser.IsKnownScheme("pack"))
                {
                    UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);
                }
                var app = new Application();
                var appResources = new Uri("pack://application:,,,/GitHub.Authentication;component/AppResources.xaml", UriKind.RelativeOrAbsolute);
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = appResources });
                app.Run(new TwoFactorWindow { DataContext = viewModel });
            })
            .Wait();
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
