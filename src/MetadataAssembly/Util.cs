using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    internal static class Util
    {
        internal const BindingFlags PublicFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        internal const BindingFlags AllFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static Dictionary<MetadataType, PrimitiveTypeCode> s_typeToPrimitive;

        private static Dictionary<PrimitiveTypeCode, MetadataType> s_primitiveToType;

        internal static int GetRowOffset(int metadataToken, int startingRow)
        {
            // The last three bytes of the metadata token is the row number (for most metadata kinds)
            return ((metadataToken << 8) >> 8) - startingRow;
        }

        internal static TokenKind GetTokenKind(int metadataToken)
        {
            // The first byte of the metadata token is the token kind.
            return (TokenKind)(metadataToken >> 24);
        }

        internal static AssemblyName GetAssemblyName(MetadataReader metadata, AssemblyReference assembly)
        {
            var name = new AssemblyName(metadata.GetString(assembly.Name));
            bool hasPublicKey = (assembly.Flags & AssemblyFlags.PublicKey) != 0;

            if (hasPublicKey)
            {
                name.Flags = AssemblyNameFlags.PublicKey;
                name.SetPublicKey(metadata.GetBlobBytes(assembly.PublicKeyOrToken));
            }
            else
            {
                name.SetPublicKeyToken(metadata.GetBlobBytes(assembly.PublicKeyOrToken));
            }

            name.CultureName = assembly.Culture.IsNil
                ? string.Empty
                : metadata.GetString(assembly.Culture);

            name.Version = assembly.Version;
            return name;
        }

        internal static CustomAttributeData[] GetCustomAttributesData(
            MetadataReader metadata,
            CustomAttributeHandleCollection attributes,
            bool inherit = false)
        {
            return new CustomAttributeData[0];
        }

        internal static bool DoTypesMatch(Type first, Type second, bool ignoreCase = false)
        {
            if (first == null || second == null)
            {
                return false;
            }

            return first.AssemblyQualifiedName.Equals(
                second.AssemblyQualifiedName,
                GetStringComparison(ignoreCase));
        }

        internal static bool IsAssemblyNameMatch(AssemblyName first, AssemblyName second, bool shouldSimpleMatch = false)
        {
            if (first == null || second == null)
            {
                return false;
            }

            if (!first.Name.Equals(second.Name, StringComparison.Ordinal))
            {
                return false;
            }

            if (shouldSimpleMatch)
            {
                return true;
            }

            return
                first.CultureName.Equals(second.CultureName, StringComparison.Ordinal) &&
                first.Version.Equals(second.Version);
        }

        internal static bool TryGetPrimitiveTypeCode(
            MetadataType type,
            out PrimitiveTypeCode typeCode)
        {
            MaybeLoadPrimitiveTables();
            return s_typeToPrimitive.TryGetValue(type, out typeCode);
        }

        internal static bool TryGetMetadataType(
            PrimitiveTypeCode typeCode,
            out MetadataType type)
        {
            MaybeLoadPrimitiveTables();
            return s_primitiveToType.TryGetValue(typeCode, out type);
        }

        internal static bool TryGetAsMetadataTypes(Type[] types, out MetadataType[] metadataTypes)
        {
            if (types == null)
            {
                metadataTypes = null;
                return true;
            }

            metadataTypes = new MetadataType[types.Length];
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i] is MetadataType metadataType)
                {
                    metadataTypes[i] = metadataType;
                    continue;
                }

                return false;
            }
            
            return true;
        }

        private static void MaybeLoadPrimitiveTables()
        {
            if (s_typeToPrimitive != null && s_primitiveToType != null)
            {
                return;
            }

            s_typeToPrimitive = new Dictionary<MetadataType, PrimitiveTypeCode>()
            {
                { MetadataDomain.GetMetadataType(typeof(bool)), PrimitiveTypeCode.Boolean },
                { MetadataDomain.GetMetadataType(typeof(byte)), PrimitiveTypeCode.Byte },
                { MetadataDomain.GetMetadataType(typeof(char)), PrimitiveTypeCode.Char },
                { MetadataDomain.GetMetadataType(typeof(double)), PrimitiveTypeCode.Double },
                { MetadataDomain.GetMetadataType(typeof(short)), PrimitiveTypeCode.Int16 },
                { MetadataDomain.GetMetadataType(typeof(int)), PrimitiveTypeCode.Int32 },
                { MetadataDomain.GetMetadataType(typeof(long)), PrimitiveTypeCode.Int64 },
                { MetadataDomain.GetMetadataType(typeof(object)), PrimitiveTypeCode.Object },
                { MetadataDomain.GetMetadataType(typeof(sbyte)), PrimitiveTypeCode.SByte },
                { MetadataDomain.GetMetadataType(typeof(float)), PrimitiveTypeCode.Single },
                { MetadataDomain.GetMetadataType(typeof(string)), PrimitiveTypeCode.String },
                { MetadataDomain.GetMetadataType(typeof(ushort)), PrimitiveTypeCode.UInt16 },
                { MetadataDomain.GetMetadataType(typeof(uint)), PrimitiveTypeCode.UInt32 },
                { MetadataDomain.GetMetadataType(typeof(ulong)), PrimitiveTypeCode.UInt64 },
                { MetadataDomain.GetMetadataType(typeof(void)), PrimitiveTypeCode.Void },
                { MetadataDomain.GetMetadataType(typeof(IntPtr)), PrimitiveTypeCode.IntPtr },
                { MetadataDomain.GetMetadataType(typeof(UIntPtr)), PrimitiveTypeCode.UIntPtr },
                { MetadataDomain.GetMetadataType(typeof(TypedReference)), PrimitiveTypeCode.TypedReference }
            };

            s_primitiveToType = s_typeToPrimitive.ToDictionary(
                pair => pair.Value,
                pair => pair.Key);
        }

        private static StringComparison GetStringComparison(bool ignoreCase)
        {
            return ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }
    }
}