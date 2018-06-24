using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace MetadataAssembly
{
    internal class MetadataParameterInfo : ParameterInfo
    {
        private readonly MetadataReader _metadata;

        private readonly ParameterHandle _handle;

        private readonly MetadataType _type;

        private readonly MethodBase _parent;

        private Parameter? _definition;

        internal MetadataParameterInfo(
            MetadataReader metadata,
            ParameterHandle handle,
            MetadataType parameterType,
            MethodBase parent)
        {
            _metadata = metadata;
            _handle = handle;
            _type = parameterType;
            _parent = parent;
        }

        public override string Name => _metadata.GetString(Definition.Name);

        public override int Position => Definition.SequenceNumber;

        public override Type ParameterType => _type;

        public override ParameterAttributes Attributes => Definition.Attributes;

        public override bool HasDefaultValue => !Definition.GetDefaultValue().IsNil;

        public override object RawDefaultValue => DefaultValue;

        public override object DefaultValue
        {
            get
            {
                var constant = _metadata.GetConstant(Definition.GetDefaultValue());
                return _metadata
                    .GetBlobReader(constant.Value)
                    .ReadConstant(constant.TypeCode);
            }
        }

        public override MemberInfo Member => _parent;

        public override int MetadataToken => MetadataTokens.GetToken(_metadata, _handle);

        private Parameter Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetParameter(_handle)).Value;
            }
        }

        public override System.Collections.Generic.IList<CustomAttributeData> GetCustomAttributesData()
        {
            return Definition
                .GetCustomAttributes()
                .Select(handle => new MetadataCustomAttributeData(_metadata, handle))
                .ToArray();
        }
    }
}