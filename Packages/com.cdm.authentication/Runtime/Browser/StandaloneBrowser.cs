using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using UnityEngine;

namespace Cdm.Authentication.Browser
{
    /// <summary>
    /// OAuth 2.0 verification browser that runs a local server and waits for a call with
    /// the authorization verification code.
    /// </summary>
    public class StandaloneBrowser : IBrowser
    {
        private TaskCompletionSource<BrowserResult> _taskCompletionSource;

        /// <summary>
        /// Gets or sets the close page response. This HTML response is shown to the user after redirection is done.
        /// </summary>
        public string closePageResponse { get; set; } = 
            "<html><body><b>DONE!</b><br>(You can close this tab/window now)</body></html>";

        public async Task<BrowserResult> StartAsync(
            string loginUrl, string redirectUrl, CancellationToken cancellationToken = default)
        {
            _taskCompletionSource = new TaskCompletionSource<BrowserResult>();

            cancellationToken.Register(() =>
            {
                _taskCompletionSource?.TrySetCanceled();
            });

            
            TcpListener tcpListener = new TcpListener(IPAddress.Any, GetPortFromUrl(redirectUrl));
            tcpListener.Start();
            try
            {

                Application.OpenURL(loginUrl);

                using (var client = await tcpListener.AcceptTcpClientAsync())
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                {
                    string request = await reader.ReadToEndAsync();
                    var responseUrl = ExtractUrlFromRequest(request);

                    _taskCompletionSource.SetResult(
                        new BrowserResult(BrowserStatus.Success, responseUrl));
                }

                return await _taskCompletionSource.Task;
            }
            finally
            {
                tcpListener.Stop();
            }
        }
        
        private int GetPortFromUrl(string url)
        {
            var uri = new Uri(url);
            return uri.Port;
        }

        private string ExtractUrlFromRequest(string request)
        {
            // Extract the URL from the request string
            // This will depend on the specific format of your request
            // Here is a very basic example:
            var lines = request.Split('\n');
            var requestLine = lines[0];
            var url = requestLine.Split(' ')[1];
            return url;
        }

        private void IncomingHttpRequest(IAsyncResult result)
        {
            var httpListener = (HttpListener)result.AsyncState;
            var httpContext = httpListener.EndGetContext(result);
            var httpRequest = httpContext.Request;
            
            // Build a response to send an "ok" back to the browser for the user to see.
            var httpResponse = httpContext.Response;
            var buffer = System.Text.Encoding.UTF8.GetBytes(closePageResponse);

            // Send the output to the client browser.
            httpResponse.ContentLength64 = buffer.Length;
            var output = httpResponse.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            _taskCompletionSource.SetResult(
                new BrowserResult(BrowserStatus.Success, httpRequest.Url.ToString()));
        }

        /// <summary>
        /// Prefixes must end in a forward slash ("/")
        /// </summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.net.httplistener?view=net-7.0#remarks" />
        private string AddForwardSlashIfNecessary(string url)
        {
            string forwardSlash = "/";
            if (!url.EndsWith(forwardSlash))
            {
                url += forwardSlash;
            }

            return url;
        }
    }
}