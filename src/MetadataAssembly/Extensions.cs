using System;
using System.Reflection;

namespace MetadataAssembly
{
    internal static class Extensions
    {
        internal static T[] Copy<T>(this T[] source)
        {
            if (source == null)
            {
                return Empty<T>.Array;
            }

            var result = new T[source.Length];
            source.CopyTo(result, 0);
            return result;
        }

        internal static bool IsMatchFull(
            this PropertyInfo property,
            string name,
            Type returnType,
            Type[] parameterTypes,
            BindingFlags flags)
        {
            if (property == null)
            {
                return false;
            }

            var getMethod = property.GetGetMethod((flags & BindingFlags.NonPublic) != 0);
            if (getMethod == null)
            {
                return false;
            }

            return property.Name.Equals(name, flags.GetComparison())
                && DoesMatchFlags(getMethod, flags)
                && IsMatchExtended(getMethod, parameterTypes, callConvention: 0)
                && Util.DoTypesMatch(returnType, getMethod.ReturnType);
        }

        internal static bool IsMatchFull(
            this MethodBase method,
            string name,
            Type[] parameterTypes,
            CallingConventions callConvention,
            BindingFlags flags)
        {
            return IsMatch(method, name, flags)
                && IsMatchExtended(method, parameterTypes, callConvention);
        }

        internal static bool IsMatchFull(
            this MethodBase method,
            Type[] parameterTypes,
            CallingConventions callConvention,
            BindingFlags flags)
        {
            return DoesMatchFlags(method, flags)
                && IsMatchExtended(method, parameterTypes, callConvention);
        }

        internal static bool IsMatch(this MemberInfo member, string name, BindingFlags flags)
        {
            return (name?.Equals(member?.Name, flags.GetComparison()) ?? false)
                && DoesMatchFlags(member, flags);
        }

        internal static bool IsMatch(this EventInfo eventInfo, string name, BindingFlags flags)
        {
            return (name?.Equals(eventInfo?.Name, flags.GetComparison()) ?? false)
                && DoesMatchFlags(eventInfo, flags);
        }

        internal static bool IsMatch(this Type type, string name, BindingFlags flags)
        {
            return (name?.Equals(type?.Name, flags.GetComparison()) ?? false)
                && DoesMatchFlags(type, flags);
        }

        internal static bool IsMatch(this FieldInfo field, string name, BindingFlags flags)
        {
            return (name?.Equals(field?.Name, flags.GetComparison()) ?? false)
                && DoesMatchFlags(field, flags);
        }

        internal static bool DoesMatchFlags(this MemberInfo member, BindingFlags flags)
        {
            if (member is MethodBase method)
            {
                return DoesMatchFlags(method, flags);
            }

            if (member is PropertyInfo property)
            {
                return DoesMatchFlags(property, flags);
            }

            if (member is FieldInfo field)
            {
                return DoesMatchFlags(field, flags);
            }

            if (member is TypeInfo type)
            {
                return DoesMatchFlags(type, flags);
            }

            if (member is EventInfo eventInfo)
            {
                return DoesMatchFlags(eventInfo, flags);
            }

            return false;
        }

        internal static bool DoesMatchFlags(this MethodBase method, BindingFlags flags)
        {
            if (method == null)
            {
                return false;
            }

            if ((flags & BindingFlags.Static) == 0 && method.IsStatic)
            {
                return false;
            }

            if ((flags & BindingFlags.Instance) == 0 && !method.IsStatic)
            {
                return false;
            }

            if ((flags & BindingFlags.NonPublic) == 0 && !method.IsPublic)
            {
                return false;
            }

            if ((flags & BindingFlags.Public) == 0 && method.IsPublic)
            {
                return false;
            }

            return true;
        }

        internal static bool DoesMatchFlags(this FieldInfo field, BindingFlags flags)
        {
            if ((flags & BindingFlags.Static) == 0 && field.IsStatic)
            {
                return false;
            }

            if ((flags & BindingFlags.Instance) == 0 && !field.IsStatic)
            {
                return false;
            }

            if ((flags & BindingFlags.NonPublic) == 0 && !field.IsPublic)
            {
                return false;
            }

            if ((flags & BindingFlags.Public) == 0 && field.IsPublic)
            {
                return false;
            }

            return true;
        }

        internal static bool DoesMatchFlags(this Type type, BindingFlags flags)
        {
            if ((flags & BindingFlags.NonPublic) == 0 && !(type.IsPublic || type.IsNestedPublic))
            {
                return false;
            }

            if ((flags & BindingFlags.Public) == 0 && (type.IsPublic || type.IsNestedPublic))
            {
                return false;
            }

            return true;
        }

        internal static bool DoesMatchFlags(this PropertyInfo property, BindingFlags flags)
        {
            return DoesMatchFlags(property.GetGetMethod(), flags);
        }

        internal static bool DoesMatchFlags(this EventInfo eventInfo, BindingFlags flags)
        {
            return DoesMatchFlags(
                eventInfo.GetAddMethod(nonPublic: (flags & BindingFlags.NonPublic) != 0),
                flags);
        }

        internal static StringComparison GetComparison(this BindingFlags flags)
        {
            return
                (flags & BindingFlags.IgnoreCase) == 0
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
        }

        private static bool IsMatchExtended(
            MethodBase method,
            Type[] parameterTypes,
            CallingConventions callConvention)
        {
            if (method == null)
            {
                return false;
            }

            if (parameterTypes != null)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                {
                    return false;
                }

                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    if (!Util.DoTypesMatch(parameters[i].ParameterType, parameterTypes[i]))
                    {
                        return false;
                    }
                }
            }

            if (!(callConvention == CallingConventions.Any || callConvention == 0) &&
                callConvention != method.CallingConvention)
            {
                return false;
            }

            return true;
        }
    }
}