namespace Microsoft.Alm.Authentication
{
    public interface IAuthentication
    {
        void DeleteCredentials(TargetUri targetUri);
        bool GetCredentials(TargetUri targetUri, out Credential credentials);
        bool SetCredentials(TargetUri targetUri, Credential credentials);
    }
}
