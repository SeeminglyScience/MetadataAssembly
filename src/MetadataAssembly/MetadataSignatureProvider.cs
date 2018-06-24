using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    internal class MetadataSignatureProvider : ISignatureTypeProvider<MetadataType, GenericContext>, ICustomAttributeTypeProvider<MetadataType>
    {
        public MetadataType GetArrayType(MetadataType elementType, ArrayShape shape)
        {
            return new MetadataArrayType(elementType, shape.Rank);
        }

        public MetadataType GetByReferenceType(MetadataType elementType)
        {
            return new MetadataByRefType(elementType);
        }

        public MetadataType GetFunctionPointerType(MethodSignature<MetadataType> signature)
        {
            throw new System.NotImplementedException();
        }

        public MetadataType GetGenericInstantiation(MetadataType genericType, ImmutableArray<MetadataType> typeArguments)
        {
            return new MetadataGenericConstructedType(
                (MetadataAssembly)genericType.Assembly,
                genericType,
                typeArguments.ToArray());
        }

        public MetadataType GetGenericMethodParameter(GenericContext genericContext, int index)
        {
            return (MetadataType)genericContext.DeclaringMethod.GetGenericArguments()[index];
        }

        public MetadataType GetGenericTypeParameter(GenericContext genericContext, int index)
        {
            return (MetadataType)genericContext.DeclaringType.GetGenericArguments()[index];
        }

        public MetadataType GetModifiedType(MetadataType modifier, MetadataType unmodifiedType, bool isRequired)
        {
            return unmodifiedType;
        }

        public MetadataType GetPinnedType(MetadataType elementType)
        {
            return elementType;
        }

        public MetadataType GetPointerType(MetadataType elementType)
        {
            return new MetadataPointerType(elementType);
        }

        public MetadataType GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            if (Util.TryGetMetadataType(typeCode, out MetadataType resolvedType))
            {
                return resolvedType;
            }

            throw new System.ArgumentException(
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "Unexpected PrimitiveTypeCode '{0}'.",
                    typeCode),
                nameof(typeCode));
        }

        public MetadataType GetSystemType()
        {
            return MetadataDomain.GetMetadataType(typeof(System.Type));
        }

        public MetadataType GetSZArrayType(MetadataType elementType)
        {
            return new MetadataArrayType(elementType, 1);
        }

        public MetadataType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            return MetadataDomain.GetMetadataType(reader, handle);
        }

        public MetadataType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            return MetadataDomain.GetMetadataType(reader, handle);
        }

        public MetadataType GetTypeFromSerializedName(string name)
        {
            return MetadataDomain.GetMetadataType(name);
        }

        public MetadataType GetTypeFromSpecification(
            MetadataReader reader,
            GenericContext genericContext,
            TypeSpecificationHandle handle,
            byte rawTypeKind)
        {
            return reader
                .GetTypeSpecification(handle)
                .DecodeSignature(this, genericContext);
        }

        public PrimitiveTypeCode GetUnderlyingEnumType(MetadataType type)
        {
            Util.TryGetPrimitiveTypeCode(
                (MetadataType)type.GetEnumUnderlyingType(),
                out PrimitiveTypeCode typeCode);
            return typeCode;
        }

        public bool IsSystemType(MetadataType type)
        {
            return type == MetadataDomain.GetMetadataType(typeof(System.Type));
        }
    }
}