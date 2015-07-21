using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface ITokenStore
    {
        /// <summary>
        /// Deletes a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token is being deleted.</param>
        void DeleteToken(Uri targetUri);
        /// <summary>
        /// Reads a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token to read.</param>
        /// <param name="token">A <see cref="Token"/> if successful; otherwise false.</param>
        /// <returns>True if successful; otherwise false.</returns>
        bool ReadToken(Uri targetUri, out Token token);
        /// <summary>
        /// Writes a <see cref="Token"/> to the underlying storage.
        /// </summary>
        /// <param name="targetUri">
        /// Unique identifier for the token, used when reading back from storage.
        /// </param>
        /// <param name="token">The <see cref="Token"/> to writen.</param>
        void WriteToken(Uri targetUri, Token token);
    }
}
