using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitHub.Authentication.ViewModels;
using GitHub.UI;
using Microsoft.Alm.Authentication;

namespace GitHub.Authentication
{
    public static class AuthenticationPrompts
    {
        public static bool CredentialModalPrompt(TargetUri targetUri, out string username, out string password)
        {
            Trace.WriteLine("Program::GithubCredentialModalPrompt");

            var credentialViewModel = new CredentialsViewModel();

            bool credentialValid = ShowViewModel(credentialViewModel, () => new CredentialsWindow());

            username = credentialViewModel.Login;
            password = credentialViewModel.Password;

            return credentialValid;
        }

        public static bool AuthenticationCodeModalPrompt(TargetUri targetUri, GithubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            Trace.WriteLine("Program::GithubAuthcodeModalPrompt");

            var twoFactorViewModel = new TwoFactorViewModel(resultType == GithubAuthenticationResultType.TwoFactorSms);

            Trace.WriteLine("   prompting user for authentication code.");

            bool authenticationCodeValid = ShowViewModel(twoFactorViewModel, () => new TwoFactorWindow());

            authenticationCode = authenticationCodeValid
                ? twoFactorViewModel.AuthenticationCode
                : null;

            return authenticationCodeValid;
        }

        static bool ShowViewModel(DialogViewModel viewModel, Func<AuthenticationDialogWindow> windowCreator)
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
                var window = windowCreator();
                window.DataContext = viewModel;
                app.Run(window);
            })
            .Wait();

            return viewModel.Result == AuthenticationDialogResult.Ok
                && viewModel.IsValid; ;
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
