using System;
using System.Runtime.InteropServices;

namespace InjectionPayload.Interop {
#pragma warning disable IDE1006 // Naming Styles

    // Layout taken from cef 88.2.9 - include\internal\cef_string_types.h
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal unsafe struct _cef_string_utf16_t {
        internal char* _str;
        internal UIntPtr _length;
        internal IntPtr _dtor;

        [DllImport("libcef.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cef_string_userfree_utf16_free(_cef_string_utf16_t* str);
    }
}
