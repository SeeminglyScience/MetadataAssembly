namespace MetadataAssembly
{
    internal class MetadataPointerType : MetadataEmptyType
    {
        internal MetadataPointerType(MetadataType elementType) : base(elementType)
        {
        }

        public override string Name => string.Concat(GetElementType().Name, "*");

        protected override bool IsPointerImpl() => true;
    }
}