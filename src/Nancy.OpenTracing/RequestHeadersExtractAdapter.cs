using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenTracing;
using OpenTracing.Propagation;

namespace Nancy.OpenTracing
{
    /// <summary>
    /// Responsible for extracting <see cref="ISpanContext"/> from Nancy HTTP Headers.
    /// </summary>
    internal sealed class RequestHeadersExtractAdapter : ITextMap
    {
        private readonly RequestHeaders _headers;

        public RequestHeadersExtractAdapter(RequestHeaders headers)
        {
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        public void Set(string key, string value)
        {
            throw new NotSupportedException("This class should only be used with ITracer.Extract");
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var kvp in _headers)
            {
                yield return new KeyValuePair<string, string>(kvp.Key, kvp.Value.FirstOrDefault());
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}