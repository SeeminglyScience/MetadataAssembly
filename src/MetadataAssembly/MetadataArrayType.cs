using System;
using System.Text;

namespace MetadataAssembly
{
    /// <summary>
    /// Represents a <see cref="MetadataType" /> where <see cref="Type.IsArray" />
    /// is <see langword="true" />.
    /// </summary>
    internal class MetadataArrayType : MetadataEmptyType
    {
        private static MetadataType s_arrayType;

        private readonly MetadataType _elementType;

        private readonly int _rank;

        private string _name;

        internal MetadataArrayType(MetadataType elementType, int rank) : base(elementType)
        {
            _elementType = elementType;
            _rank = rank;
        }

        public override string Name => GetName();

        public override int MetadataToken => 0x02000000;

        private string GetName()
        {
            if (!string.IsNullOrEmpty(_name))
            {
                return _name;
            }

            return _name = new StringBuilder(_elementType.Name.Length + 2 + _rank - 1)
                .Append(_elementType.Name)
                .Append('[')
                .Append(',', _rank - 1)
                .Append(']')
                .ToString();
        }

        public override Type BaseType =>
            s_arrayType ?? (s_arrayType = MetadataDomain.GetMetadataType(typeof(Array)));

        public override int GetArrayRank() => _rank;

        protected override bool IsArrayImpl() => true;

        protected override Type[] GetInterfacesImpl()
        {
            return new Type[]
            {
                MetadataDomain.GetMetadataType(typeof(ICloneable)),
                MetadataDomain.GetMetadataType(typeof(System.Collections.IList)),
                MetadataDomain.GetMetadataType(typeof(System.Collections.ICollection)),
                MetadataDomain.GetMetadataType(typeof(System.Collections.IEnumerable)),
                MetadataDomain.GetMetadataType(typeof(System.Collections.IStructuralComparable)),
                MetadataDomain.GetMetadataType(typeof(System.Collections.IStructuralEquatable)),
                MetadataDomain.GetMetadataType(typeof(System.Collections.Generic.IList<>)).MakeGenericType(GetElementType()),
                MetadataDomain.GetMetadataType(typeof(System.Collections.Generic.ICollection<>)).MakeGenericType(GetElementType()),
                MetadataDomain.GetMetadataType(typeof(System.Collections.Generic.IEnumerable<>)).MakeGenericType(GetElementType()),
                MetadataDomain.GetMetadataType(typeof(System.Collections.Generic.IReadOnlyList<>)).MakeGenericType(GetElementType()),
                MetadataDomain.GetMetadataType(typeof(System.Collections.Generic.IReadOnlyCollection<>)).MakeGenericType(GetElementType())
            };
        }
    }
}