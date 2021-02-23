using System;
using InjectionPayload.Interop;

namespace InjectionPayload {
    internal unsafe readonly struct CefStringUserfree : IDisposable {
        private readonly _cef_string_utf16_t* underlying;

        public CefStringUserfree(_cef_string_utf16_t* str) {
            underlying = str;
        }

        public override string? ToString() {
            if (underlying is null)
                return null;
            return new string(underlying->_str, 0, (int)underlying->_length);
        }

        public void Dispose() {
            if (underlying is not null)
                _cef_string_utf16_t.cef_string_userfree_utf16_free(underlying);
        }
    }
}
