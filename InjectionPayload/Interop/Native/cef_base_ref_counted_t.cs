using System;
using System.Runtime.InteropServices;

namespace InjectionPayload.Interop {
#pragma warning disable IDE1006 // Naming Styles

    /// <summary>
    /// All ref-counted framework structures must include this structure first.
    /// </summary>
    // Layout taken from cef 88.2.9 - include\capi\cef_base_capi.h
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal unsafe struct cef_base_ref_counted_t {
        /// <summary>
        /// Size of the data structure.
        /// </summary>
        internal UIntPtr _size;
        /// <summary>
        /// Called to increment the reference count for the object. Should be called
        /// for every new copy of a pointer to a given object.
        /// </summary>
        internal IntPtr _add_ref;
        /// <summary>
        /// Called to decrement the reference count for the object. If the reference
        /// count falls to 0 the object should self-delete. Returns true (1) if the
        /// resulting reference count is 0.
        /// </summary>
        internal IntPtr _release;
        /// <summary>
        /// Returns true (1) if the current reference count is 1.
        /// </summary>
        internal IntPtr _has_one_ref;
        /// <summary>
        /// Returns true (1) if the current reference count is at least 1.
        /// </summary>
        internal IntPtr _has_at_least_one_ref;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate int add_ref_delegate(cef_base_ref_counted_t* self);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate int release_delegate(cef_base_ref_counted_t* self);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate int get_refct_delegate(cef_base_ref_counted_t* self);
    }
}
