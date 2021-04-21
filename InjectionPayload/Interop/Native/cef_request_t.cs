using System;
using System.Runtime.InteropServices;

namespace InjectionPayload.Interop {
#pragma warning disable IDE1006 // Naming Styles

    /// <summary>
    /// Structure used to represent a web request. The functions of this structure may be called on any thread.
    /// </summary>
    // Layout taken from cef 88.2.9 - include\capi\cef_request_capi.h
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal unsafe struct cef_request_t {
        /// <summary>
        /// Base structure.
        /// </summary>
        internal cef_base_ref_counted_t _base;
        /// <summary>
        /// Returns true (1) if this object is read-only.
        /// </summary>
        internal IntPtr _is_read_only;
        /// <summary>
        /// <para>Get the fully qualified URL.</para>
        /// <para>The resulting string must be freed by calling cef_string_userfree_free().</para>
        /// </summary>
        internal IntPtr _get_url;
        /// <summary>
        /// Set the fully qualified URL.
        /// </summary>
        internal IntPtr _set_url;
        /// <summary>
        /// <para>
        /// Get the request function type.
        /// The value will default to POST if post data is provided and GET otherwise.
        /// </para>
        /// <para>The resulting string must be freed by calling cef_string_userfree_free().</para>
        /// </summary>
        internal IntPtr _get_method;
        /// <summary>
        /// Set the request function type.
        /// </summary>
        internal IntPtr _set_method;
        /// <summary>
        /// Set the referrer URL and policy. If non-NULL the referrer URL must be fully
        /// qualified with an HTTP or HTTPS scheme component. Any username, password or
        /// ref component will be removed.
        /// </summary>
        internal IntPtr _set_referrer;
        /// <summary>
        /// <para>Get the referrer URL.</para>
        /// <para>The resulting string must be freed by calling cef_string_userfree_free().</para>
        /// </summary>
        internal IntPtr _get_referrer_url;
        /// <summary>
        /// Get the referrer policy.
        /// </summary>
        internal IntPtr _get_referrer_policy;
        /// <summary>
        /// Get the post data.
        /// </summary>
        internal IntPtr _get_post_data;
        /// <summary>
        /// Set the post data.
        /// </summary>
        internal IntPtr _set_post_data;
        /// <summary>
        /// Get the header values. Will not include the Referer value if any.
        /// </summary>
        internal IntPtr _get_header_map;
        /// <summary>
        /// Set the header values. If a Referer value exists in the header map it will
        /// be removed and ignored.
        /// </summary>
        internal IntPtr _set_header_map;
        /// <summary>
        /// <para>
        /// Returns the first header value for |name| or an NULL string if not found.
        /// Will not return the Referer value if any. Use GetHeaderMap instead if
        /// |name| might have multiple values.
        /// </para>
        /// <para>The resulting string must be freed by calling cef_string_userfree_free().</para>
        /// </summary>
        internal IntPtr _get_header_by_name;
        /// <summary>
        /// Set the header |name| to |value|. If |overwrite| is true (1) any existing
        /// values will be replaced with the new value. If |overwrite| is false (0) any
        /// existing values will not be overwritten. The Referer value cannot be set
        /// using this function.
        /// </summary>
        internal IntPtr _set_header_by_name;
        /// <summary>
        /// Set all values at one time.
        /// </summary>
        internal IntPtr _set;
        /// <summary>
        /// Get the flags used in combination with cef_urlrequest_t. See
        /// cef_urlrequest_flags_t for supported values.
        /// </summary>
        internal IntPtr _get_flags;
        /// <summary>
        /// Set the flags used in combination with cef_urlrequest_t.  See
        /// cef_urlrequest_flags_t for supported values.
        /// </summary>
        internal IntPtr _set_flags;
        /// <summary>
        /// <para>
        /// Get the URL to the first party for cookies used in combination with
        /// cef_urlrequest_t.
        /// </para>
        /// <para>The resulting string must be freed by calling cef_string_userfree_free().</para>
        /// </summary>
        internal IntPtr _get_first_party_for_cookies;
        /// <summary>
        /// Set the URL to the first party for cookies used in combination with
        /// cef_urlrequest_t.
        /// </summary>
        internal IntPtr _set_first_party_for_cookies;
        /// <summary>
        /// Get the resource type for this request. Only available in the browser
        /// process.
        /// </summary>
        internal IntPtr _get_resource_type;
        /// <summary>
        /// Get the transition type for this request. Only available in the browser
        /// process and only applies to requests that represent a main frame or sub-
        /// frame navigation.
        /// </summary>
        internal IntPtr _get_transition_type;
        /// <summary>
        /// Returns the globally unique identifier for this request or 0 if not
        /// specified. Can be used by cef_resource_request_handler_t implementations in
        /// the browser process to track a single request across multiple callbacks.
        /// </summary>
        internal IntPtr _get_identifier;

        /// <summary>
        /// Create a new cef_request_t object.
        /// </summary>
        [DllImport("libcef.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern cef_request_t* cef_request_create();

        public static _cef_string_utf16_t* get_url(cef_request_t* self) {
            var func = Marshal.GetDelegateForFunctionPointer<get_url_delegate>(self->_get_url);
            return func(self);
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate _cef_string_utf16_t* get_url_delegate(cef_request_t* self);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void set_url_delegate(cef_request_t* self, _cef_string_utf16_t* url);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate _cef_string_utf16_t* get_method_delegate(cef_request_t* self);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void set_method_delegate(cef_request_t* self, _cef_string_utf16_t* method);
    }
}
