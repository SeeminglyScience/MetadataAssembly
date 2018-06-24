using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    internal class MetadataMethodInfo : MethodInfo
    {
        private readonly MetadataType _type;

        private readonly MetadataReader _metadata;

        private MethodSignature<MetadataType>? _signature;

        private MethodDefinition? _definition;

        private string _name;

        private MetadataParameterInfo[] _parameters;

        private MetadataType[] _genericParameters;

        internal MetadataMethodInfo(MetadataType type, MethodDefinitionHandle handle)
        {
            _type = type;
            Handle = handle;
            _metadata = ((MetadataAssembly)type.Assembly).Metadata;
        }

        internal MetadataMethodInfo(
            MetadataType type,
            MethodDefinitionHandle handle,
            MethodDefinition definition,
            string name)
        {
            _type = type;
            Handle = handle;
            _metadata = ((MetadataAssembly)type.Assembly).Metadata;
            _definition = definition;
            _name = name;
        }

        public override bool IsGenericMethod => Signature.GenericParameterCount != 0;

        public override bool IsGenericMethodDefinition => IsGenericMethod;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => ReturnType;

        public override MethodAttributes Attributes => Definition.Attributes;

        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

        public override Type DeclaringType => _type;

        public override Type ReturnType => Signature.ReturnType;

        public override string Name =>
            string.IsNullOrEmpty(_name)
                ? _name = _metadata.GetString(Definition.Name)
                : _name;

        public override Type ReflectedType => _type;

        internal MethodDefinitionHandle Handle { get; }

        private MethodDefinition Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetMethodDefinition(Handle)).Value;
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
                    new GenericContext()
                    {
                        DeclaringMethod = this,
                        DeclaringType = _type
                    });

                return _signature.Value;
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
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

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetGenericArguments()
        {
            if (_genericParameters != null)
            {
                return _genericParameters.Copy();
            }

            var parameters = Definition.GetGenericParameters()
                .Select(handle =>
                    new MetadataGenericParameterType(
                        (MetadataAssembly)_type.Assembly,
                        handle,
                        _type,
                        this))
                .ToArray();
            
            _genericParameters = parameters;
            return _genericParameters.Copy();
        }

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            if (!IsGenericMethod)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "{0} is not a GenericMethodDefinition. MakeGenericMethod may only be called on a type for which MethodBase.IsGenericMethodDefinition is true.",
                        Name));
            }

            Type[] parameters = GetGenericArguments();
            if (typeArguments == null || typeArguments.Length != parameters.Length)
            {
                int argumentLength = typeArguments?.Length ?? 0;
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "The type or method has {0} generic parameter(s), but {1} generic argument(s) were provided. A generic argument must be provided for each generic parameter.",
                        parameters.Length,
                        argumentLength),
                    nameof(typeArguments));
            }

            var instantiation = new MetadataType[typeArguments.Length];
            for (var i = 0; i < typeArguments.Length; i++)
            {
                if (typeArguments[i] is MetadataType metadataType)
                {
                    instantiation[i] = metadataType;
                    continue;
                }

                throw new ArgumentException(
                    string.Format(
                    CultureInfo.CurrentCulture,
                    "The type argument '{0}' is not inherited from '{1}'. Metadata types must be instantiated with metadata types.",
                    typeArguments[i].ToString(),
                    nameof(MetadataType)));
            }

            return new MetadataConstructedGenericMethodInfo(
                _type,
                this,
                instantiation);
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
            var definition = new System.Text.StringBuilder(ReturnType.ToString())
                .Append(' ')
                .Append(Name);

            if (IsGenericMethod)
            {
                definition.Append('[');
                Type[] genericArgs = GetGenericArguments();
                for (var i = 0; i < genericArgs.Length; i++)
                {
                    definition.Append(genericArgs[i].ToString());

                    if (i != genericArgs.Length - 1)
                    {
                        definition.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator);
                    }
                }

                definition.Append(']');
            }

            definition.Append('(');

            ParameterInfo[] parameters = GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                definition.Append(parameters[i].ParameterType.ToString());

                if (i != parameters.Length - 1)
                {
                    definition.Append(CultureInfo.CurrentCulture.TextInfo.ListSeparator).Append(' ');
                }
            }

            return definition.Append(')').ToString();
        }
    }
}