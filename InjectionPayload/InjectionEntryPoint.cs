using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using BurntSushi.Shared;
using EasyHook;
using InjectionPayload.Interop;
using WildcardMatch;

namespace InjectionPayload {
    public class MySimpleEntryPoint : IEntryPoint {
        private readonly AbstractHookInterface _server;

        public MySimpleEntryPoint(RemoteHooking.IContext _, string channelName) {
            _server = RemoteHooking.IpcConnectClient<AbstractHookInterface>(channelName);
            _server.Ping();
        }

        public unsafe void Run(RemoteHooking.IContext _, string __) {
            _server.LogInfo("Injection succedded...");

            LocalHook? createRequestHook = null;
            LocalHook? getAdressInfoHook = null;

            try {
                _server.LogInfo("Installing hooks...");

                createRequestHook = LocalHook.Create(
                    LocalHook.GetProcAddress("libcef.dll", nameof(cef_urlrequest_create)),
                    new cef_urlrequest_create_delegate(cef_urlrequest_create_hook),
                    this);
                getAdressInfoHook = LocalHook.Create(
                    LocalHook.GetProcAddress("WS2_32.dll", nameof(getaddrinfo)),
                    new getaddrinfo_delegate(get_addr_info_hook),
                    this);

                // activate hooks (exclude current thread)
                createRequestHook?.ThreadACL.SetExclusiveACL(new int[] { 0 });
                getAdressInfoHook?.ThreadACL.SetExclusiveACL(new int[] { 0 });

                _server.LogInfo("Hooks installed");

                try {
                    while (true) {
                        Thread.Sleep(5000);
                        _server.Ping();
                    }
                } catch {
                }
            } catch (Exception e) {
                TryLogException(e);
            } finally {
                createRequestHook?.Dispose();
                getAdressInfoHook?.Dispose();
                LocalHook.Release();
            }
        }

        #region Hooks
#pragma warning disable IDE1006 // Naming Styles
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe delegate IntPtr cef_urlrequest_create_delegate(cef_request_t* request, IntPtr client, IntPtr request_context);

        [DllImport("libcef.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, SetLastError = true)]
        private unsafe static extern IntPtr cef_urlrequest_create(cef_request_t* request, IntPtr client, IntPtr request_context);

        private unsafe IntPtr cef_urlrequest_create_hook(cef_request_t* request, IntPtr client, IntPtr request_context) {
            try {
                var req = new CefRequest(request);
                var url = req.GetUrl();
                if (url != null) {
                    var block = RequestFilter.Blacklist.Any(pattern => pattern.WildcardMatch(url, true));
                    LogRequest(nameof(cef_urlrequest_create), url, block);
                    if (block)
                        return IntPtr.Zero;
                }
            } catch (Exception e) {
                // swallow exceptions so that any issues caused by this code do not crash target process
                TryLogException(e);
            }

            // now call the original API...
            return cef_urlrequest_create(request, client, request_context);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int getaddrinfo_delegate(IntPtr node, IntPtr service, IntPtr hints, IntPtr res);

        [DllImport("WS2_32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int getaddrinfo(IntPtr node, IntPtr service, IntPtr hints, IntPtr res);

        private int get_addr_info_hook(IntPtr node, IntPtr service, IntPtr hints, IntPtr res) {
            try {
                var url = Marshal.PtrToStringAnsi(node);

                var block = !RequestFilter.Whitelist.Any(pattern => pattern.WildcardMatch(url, true));
                LogRequest(nameof(getaddrinfo), url, block);
                if (block)
                    return 0;
            } catch (Exception e) {
                // swallow exceptions so that any issues caused by this code do not crash target process
                TryLogException(e);
            }

            // now call the original API...
            return getaddrinfo(node, service, hints, res);
        }
#pragma warning restore IDE1006 // Naming Styles
        #endregion Hooks

        #region Log Helpers
        [SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.",
            Justification = "Only used if error during error reporting; exception would crash the host app")]
        private void TryLogException(Exception e) {
            try {
                _server.LogException(e);
            } catch (Exception) {
                // we dont want to crash the app if somehow we throw while doing error reporting.
            }
        }

        private void LogRequest(string hook, string url, bool blocked) {
            _server.LogInfo($"[{(blocked ? "-" : "+")}] ({hook}) {url}");
        }
        #endregion
    }
}
