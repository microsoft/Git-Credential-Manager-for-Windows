using Core.Authentication.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Bitbucket.Authentication.OAuth
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleServer
    {
        /// <summary>
        /// Async wait for an URL with a timeout
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="RemoteHostException">Throws when there's a timeout, or other error</exception>
        public static async Task<string> WaitForURLAsync(string url, CancellationToken cancellationToken)
        {
            var listener = new HttpListener { Prefixes = { url } };
            listener.Start();

            string rawUrl = "";
            try
            {
                var context = await listener.GetContextAsync().RunWithCancellation(cancellationToken);
                rawUrl = context.Request.RawUrl;

                await Task.Delay(100); //Wait 100ms before writing the response out

                //Serve back a simple auth message.
                var html = GetSuccessString();
                context.Response.ContentType = "text/html";
                context.Response.OutputStream.WriteStringUtf8(html);

                await Task.Delay(100); //Wait 100ms before writing the response out

                context.Response.Close();
            }
            catch (TimeoutException ex)
            {
                // throw new ConnectivityException("Timeout awaiting incoming request.", ex);
                throw new Exception("Timeout awaiting incoming request.", ex);
            }
            catch (Exception ex)
            {
                //Some other error?
                //throw new ConnectivityException("Failure awating incoming request.", ex);
                throw new Exception("Failure awating incoming request.", ex);
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }

            return rawUrl;
        }

        private static string GetSuccessString()
        {
            var result = "Auth Successful. You may now close this page";

            try
            {
                if (!UriParser.IsKnownScheme("pack"))
                    new System.Windows.Application();


                var html = Application.GetResourceStream(
            new Uri("pack://application:,,,/Bitbucket.Authentication;component/Assets/auth.html", UriKind.Absolute));
                if (html != null)
                {
                    using (StreamReader reader = new StreamReader(html.Stream))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return result;
            }

            return result;
        }
    }

}
