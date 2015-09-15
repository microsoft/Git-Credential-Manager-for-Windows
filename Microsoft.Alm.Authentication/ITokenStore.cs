using System;

namespace Microsoft.Alm.Authentication
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
        /// <param name="token">A <see cref="Token"/> if successful; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if successful; otherwise <see langword="false"/>.</returns>
        bool ReadToken(Uri targetUri, out Token token);
        /// <summary>
        /// Writes a <see cref="Token"/> to the underlying storage.
        /// </summary>
        /// <param name="targetUri">
        /// Unique identifier for the token, used when reading back from storage.
        /// </param>
        /// <param name="token">The <see cref="Token"/> to be written.</param>
        void WriteToken(Uri targetUri, Token token);
    }
}
