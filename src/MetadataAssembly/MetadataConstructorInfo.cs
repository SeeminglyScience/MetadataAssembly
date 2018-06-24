using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    /// <summary>
    /// Represents a reflection only <see cref="ConstructorInfo" /> implemented
    /// using <see cref="MetadataReader" />.
    /// </summary>
    internal class MetadataConstructorInfo : ConstructorInfo
    {
        private readonly MetadataType _type;

        private readonly MetadataReader _metadata;

        private readonly MethodDefinitionHandle _handle;

        private MethodDefinition? _definition;

        private MethodSignature<MetadataType>? _signature;

        private MetadataParameterInfo[] _parameters;

        private string _name;

        internal MetadataConstructorInfo(MetadataType type, MethodDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;
            _metadata = ((MetadataAssembly)type.Assembly).Metadata;
        }

        internal MetadataConstructorInfo(
            MetadataType type,
            MethodDefinitionHandle handle,
            MethodDefinition definition,
            string name)
        {
            _type = type;
            _handle = handle;
            _metadata = ((MetadataAssembly)type.Assembly).Metadata;
            _definition = definition;
            _name = name;
        }

        public override MethodAttributes Attributes => Definition.Attributes;

        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

        public override Type DeclaringType => _type;

        public override string Name => _metadata.GetString(Definition.Name);

        public override Type ReflectedType => _type;

        private MethodDefinition Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetMethodDefinition(_handle)).Value;
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

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return Definition.ImplAttributes;
        }

        public override ParameterInfo[] GetParameters()
        {
            MetadataType[] parameterTypes = Signature.ParameterTypes.ToArray();
            ParameterHandle[] handles = Definition.GetParameters().ToArray();
            var parameters = new MetadataParameterInfo[handles.Length];
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                parameters[i] = new MetadataParameterInfo(
                    _metadata,
                    handles[i],
                    parameterTypes[i],
                    this);
            }

            _parameters = parameters;
            return _parameters.Copy();
        }

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
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
            var definition = new System.Text.StringBuilder("Void")
                .Append(' ')
                .Append(Name)
                .Append('(');

            ParameterInfo[] parameters = GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                definition.Append(parameters[i].ParameterType.ToString());

                if (i != parameters.Length - 1)
                {
                    definition
                        .Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator)
                        .Append(' ');
                }
            }

            return definition.Append(')').ToString();
        }
    }
}