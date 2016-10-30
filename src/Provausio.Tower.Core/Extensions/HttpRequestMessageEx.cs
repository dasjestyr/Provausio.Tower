using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Provausio.Tower.Core.Extensions
{
    public static class HttpRequestMessageEx
    {
        public static async Task<HttpRequestMessage> Clone(this HttpRequestMessage message)
        {
            var clone = new HttpRequestMessage(message.Method, message.RequestUri);

            // Copy the request's content (via a MemoryStream) into the cloned object
            var ms = new MemoryStream();
            if (message.Content != null)
            {
                await message.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                // Copy the content headers
                if (message.Content.Headers != null)
                    foreach (var h in message.Content.Headers)
                        clone.Content.Headers.Add(h.Key, h.Value);
            }

            clone.Version = message.Version;

            foreach (var prop in message.Properties)
                clone.Properties.Add(prop);

            foreach (var header in message.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }

        public static async Task<HttpContent> Clone(this HttpContent content)
        {
            var ms = new MemoryStream();
            if(content == null)
                throw new ArgumentNullException(nameof(content));
            
            await content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;
            return new StreamContent(ms);
        }
    }
}
