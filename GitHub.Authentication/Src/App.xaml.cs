using System.Windows;

namespace GitHub.Authentication
{
    /// <summary>
    /// <para>Application used to test the GitHub Two Factor dialog.</para>
    /// <para>
    /// The Git Credential Helper directly references this assembly and instantiates the 2fa dialog
    /// in process.
    /// </para>
    /// <para>
    /// It does not shell out to this exe. The exe is here simply to make working on this dialog easy.
    /// </para>
    /// </summary>
    public partial class App : Application
    {
    }
}
