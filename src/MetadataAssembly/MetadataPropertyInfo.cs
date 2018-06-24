using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    internal class MetadataPropertyInfo : PropertyInfo
    {
        private readonly MetadataType _type;

        private readonly MetadataReader _metadata;

        private readonly PropertyDefinitionHandle _handle;

        private PropertyDefinition? _definition;

        private MethodSignature<MetadataType>? _signature;

        internal MetadataPropertyInfo(MetadataType type, PropertyDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;
            _metadata = ((MetadataAssembly)type.Assembly).Metadata;
        }

        public override PropertyAttributes Attributes => Definition.Attributes;

        public override bool CanRead => !Definition.GetAccessors().Getter.IsNil;

        public override bool CanWrite => !Definition.GetAccessors().Setter.IsNil;

        public override Type PropertyType => Signature.ReturnType;

        public override Type DeclaringType => _type;

        public override string Name => _metadata.GetString(Definition.Name);

        public override Type ReflectedType => _type;

        private PropertyDefinition Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetPropertyDefinition(_handle)).Value;
            }
        }

        private MethodSignature<MetadataType> Signature
        {
            get
            {
                if (_signature != null)
                {
                    return _signature.Value;
                }

                _signature = Definition.DecodeSignature(
                    new MetadataSignatureProvider(),
                    new GenericContext() { DeclaringType = _type });

                return _signature.Value;
            }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return new MethodInfo[]
            {
                GetGetMethod(nonPublic),
                GetSetMethod(nonPublic)
            };
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return GetMethodFromParent(Definition.GetAccessors().Getter, nonPublic);
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return GetGetMethod().GetParameters();
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return GetMethodFromParent(Definition.GetAccessors().Setter, nonPublic);
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            var definition = new System.Text.StringBuilder(PropertyType.ToString())
                .Append(' ')
                .Append(Name);

            ParameterInfo[] parameters = GetIndexParameters();
            if (parameters.Length == 0)
            {
                return definition.ToString();
            }

            definition.Append(' ').Append('[');
            for (var i = 0; i < parameters.Length; i++)
            {
                definition.Append(parameters[i].ParameterType.ToString());

                if (i != parameters.Length - 1)
                {
                    definition.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator).Append(' ');
                }
            }

            return definition.Append(']').ToString();
        }

        private MethodInfo GetMethodFromParent(MethodDefinitionHandle methodHandle, bool isNonPublic)
        {
            var methodDef = _metadata.GetMethodDefinition(methodHandle);
            var flags = BindingFlags.Instance | BindingFlags.Static;
            flags |= isNonPublic ? BindingFlags.NonPublic : BindingFlags.Public;

            return _type.GetMetadataMethod(
                _metadata.GetString(methodDef.Name),
                flags);
        }
    }
}