using System;
using System.Diagnostics;
using System.Linq;
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

            Trace.WriteLine("   prompting user for credentials.");

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
                EnsureApplicationResources();
                var window = windowCreator();
                window.DataContext = viewModel;
                window.ShowDialog();
            })
            .Wait();

            return viewModel.Result == AuthenticationDialogResult.Ok
                && viewModel.IsValid;
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

        static void EnsureApplicationResources()
        {
            if (!UriParser.IsKnownScheme("pack"))
            {
                UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1);
            }

            var appResourcesUri = new Uri("pack://application:,,,/GitHub.Authentication;component/AppResources.xaml", UriKind.RelativeOrAbsolute);

            // If we launch two dialogs in the same process (Credential followed by 2fa), calling new App() 
            // throws an exception stating the Application class  can't be created twice. Creating an App
            // instance happens to set Application.Current to that instance (it's weird). However, if you
            // don't set the ShutdownMode to OnExplicitShutdown, the second time you launch a dialog, 
            // Application.Current is null even in the same process.
            if (Application.Current == null)
            {
                var app = new Application();
                Debug.Assert(Application.Current == app, "Current application not set");
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = appResourcesUri });
            }
            else
            {
                // Application.Current exists, but what if in the future, some other code created
                // the singleton. Let's make sure our resources are still loaded.
                var resourcesExist = Application.Current.Resources.MergedDictionaries.Any(r => r.Source == appResourcesUri);
                if (!resourcesExist)
                {
                    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = appResourcesUri });
                }
            }

        }
    }
}
