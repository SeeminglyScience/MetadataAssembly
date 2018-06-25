using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace MetadataAssembly
{
    internal class MetadataFieldInfo : FieldInfo
    {
        private readonly MetadataType _type;

        private readonly MetadataReader _metadata;

        private readonly FieldDefinitionHandle _handle;

        private FieldDefinition? _definition;

        internal MetadataFieldInfo(MetadataType type, FieldDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;
            _metadata = ((MetadataAssembly)type.Assembly).Metadata;
        }

        public override int MetadataToken => MetadataTokens.GetToken(_metadata, _handle);

        public override FieldAttributes Attributes => Definition.Attributes;

        public override RuntimeFieldHandle FieldHandle => throw new NotSupportedException();

        public override Type FieldType =>
            Definition.DecodeSignature(
                new MetadataSignatureProvider(),
                new GenericContext() { DeclaringType = _type });

        public override Type DeclaringType => _type;

        public override string Name => _metadata.GetString(Definition.Name);

        public override Type ReflectedType => _type;

        private FieldDefinition Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetFieldDefinition(_handle)).Value;
            }
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override System.Collections.Generic.IList<CustomAttributeData> GetCustomAttributesData()
        {
            return Definition
                .GetCustomAttributes()
                .Select(handle => new MetadataCustomAttributeData(_metadata, handle))
                .ToArray();
        }

        public override string ToString()
        {
            return new System.Text.StringBuilder(FieldType.ToString())
                .Append(' ')
                .Append(Name)
                .ToString();
        }
    }
}