using System;
using System.Reflection;

namespace BurntSushi.Extensions {
    internal static class TypeExtensions {
        public static ConstructorInfo? GetConstructor(this Type type, BindingFlags bindingFlags, Type[] types) {
            return type.GetConstructor(bindingFlags, null, types, null);
        }
    }
}
