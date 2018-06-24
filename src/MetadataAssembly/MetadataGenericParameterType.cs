using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    internal class MetadataGenericParameterType : MetadataType
    {
        private readonly bool _isMethodParameter;

        private readonly MetadataType _declaringType;

        private readonly MetadataMethodInfo _declaringMethod;

        private readonly MetadataReader _metadata;

        private readonly GenericParameterHandle _handle;

        private GenericParameter? _definition;

        internal MetadataGenericParameterType(
            MetadataAssembly assembly,
            GenericParameterHandle handle,
            MetadataType declaringType)
            : this(assembly, handle, declaringType, null)
        {
        }

        internal MetadataGenericParameterType(
            MetadataAssembly assembly,
            GenericParameterHandle handle,
            MetadataType declaringType,
            MetadataMethodInfo declaringMethod)
            : base(assembly, default(TypeDefinitionHandle))
        {
            _isMethodParameter = declaringMethod != null;
            _declaringMethod = declaringMethod;
            _declaringType = declaringType;
            _metadata = assembly.Metadata;
            _handle = handle;
        }

        public override string Name => _metadata.GetString(Definition.Name);

        public override string Namespace => string.Empty;

        public override string FullName => Name;

        public override string AssemblyQualifiedName => string.Empty;

        public override bool IsGenericParameter => true;

        public override int GenericParameterPosition => Definition.Index;

        public override GenericParameterAttributes GenericParameterAttributes => Definition.Attributes;
 
        public override Type DeclaringType => _declaringType;

        public override MethodBase DeclaringMethod => _declaringMethod;

        private GenericParameter Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetGenericParameter(_handle)).Value;
            }
        }

        public override Type[] GetGenericParameterConstraints()
        {
            return
                Definition
                    .GetConstraints()
                    .Select(
                        handle => MetadataDomain.GetMetadataType(
                            _metadata,
                            _metadata.GetGenericParameterConstraint(handle).Type))
                    .ToArray();
        }
    }
}