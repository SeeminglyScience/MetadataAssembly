using System;
using System.Reflection;

namespace MetadataAssembly
{
    /// <summary>
    /// Represents a <see cref="MetadataMethodInfo" /> that has been supplied
    /// generic type arguments.
    /// </summary>
    internal class MetadataConstructedGenericMethodInfo : MetadataMethodInfo
    {
        private readonly MetadataMethodInfo _definition;

        private readonly MetadataType _declaringType;

        private readonly MetadataType[] _typeArguments;

        internal MetadataConstructedGenericMethodInfo(
            MetadataType type,
            MetadataMethodInfo definition,
            MetadataType[] instantiation)
            : base(type, definition.Handle)
        {
            _declaringType = type;
            _definition = definition;
            _typeArguments = instantiation;
        }

        public override bool IsGenericMethod => true;

        public override bool IsGenericMethodDefinition => false;

        public override MethodInfo GetGenericMethodDefinition() => _definition;

        public override Type[] GetGenericArguments() => _typeArguments.Copy();
    }
}