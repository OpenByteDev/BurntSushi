using InjectionPayload.Interop;

namespace InjectionPayload {
    internal unsafe readonly struct CefRequest {
        private readonly cef_request_t* underlying;

        public CefRequest(cef_request_t* request) {
            underlying = request;
        }

        public string GetUrl() {
            var ptr = cef_request_t.get_url(underlying);
            using var userfree = new CefStringUserfree(ptr);
            return userfree.ToString();
        }
    }
}
