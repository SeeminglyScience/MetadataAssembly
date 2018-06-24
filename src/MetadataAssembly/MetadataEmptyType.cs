using System;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    internal abstract class MetadataEmptyType : MetadataType
    {
        private readonly MetadataType _elementType;

        internal MetadataEmptyType(MetadataType elementType)
            : base((MetadataAssembly)elementType.Assembly, default(TypeDefinitionHandle))
        {
            _elementType = elementType;
        }

        public override abstract string Name { get; }

        public override Type GetElementType() => _elementType;

        protected override bool HasElementTypeImpl() => true;

        protected override Type[] GetInterfacesImpl()
        {
            return Empty<Type>.Array;
        }
    }
}