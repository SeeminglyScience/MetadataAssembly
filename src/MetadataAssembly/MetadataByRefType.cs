namespace MetadataAssembly
{
    /// <summary>
    /// Represents a <see cref="MetadataType" /> where <see cref="System.Type.IsByRef" />
    /// is <see langword="true" />.
    /// </summary>
    internal class MetadataByRefType : MetadataEmptyType
    {
        internal MetadataByRefType(MetadataType elementType) : base(elementType)
        {
        }

        public override string Name => string.Concat(GetElementType().Name, "&");

        protected override bool IsByRefImpl() => true;
    }
}