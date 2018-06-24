using System;

namespace MetadataAssembly
{
    internal class MetadataGenericConstructedType : MetadataType
    {
        private readonly MetadataType[] _typeArguments;

        private readonly MetadataType _genericDefinition;

        internal MetadataGenericConstructedType(
            MetadataAssembly assembly,
            MetadataType definition,
            MetadataType[] instantiation)
            : base(assembly, definition.Handle)
        {
            _typeArguments = instantiation;
            _genericDefinition = definition;
        }

        public override Type[] GetGenericArguments() => _typeArguments.Copy();

        public override bool IsGenericType => true;

        public override bool IsGenericTypeDefinition => false;

        public override bool IsConstructedGenericType => true;

        public override Type GetGenericTypeDefinition() => _genericDefinition;
    }
}