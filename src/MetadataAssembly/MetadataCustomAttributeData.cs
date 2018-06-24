using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace MetadataAssembly
{
    /// <summary>
    /// Represents a reflection only <see cref="CustomAttributeData" /> implemented
    /// using <see cref="MetadataReader" />
    /// </summary>
    internal class MetadataCustomAttributeData : CustomAttributeData
    {
        private readonly MetadataReader _metadata;

        private readonly CustomAttributeHandle _handle;
        
        private CustomAttribute? _definition;

        private CustomAttributeValue<MetadataType>? _value;

        private ConstructorInfo _constructor;

        private CustomAttributeTypedArgument[] _fixedArguments;

        private CustomAttributeNamedArgument[] _namedArguments;

        internal MetadataCustomAttributeData(MetadataReader metadata, CustomAttributeHandle handle)
        {
            _metadata = metadata;
            _handle = handle;
        }

        public override ConstructorInfo Constructor => GetConstructor();

        public override IList<CustomAttributeTypedArgument> ConstructorArguments => GetFixedArguments();

        public override IList<CustomAttributeNamedArgument> NamedArguments => GetNamedArguments();

        private CustomAttributeValue<MetadataType> Value
        {
            get
            {
                if (_value != null)
                {
                    return _value.Value;
                }

                return (_value = Definition.DecodeValue(new MetadataSignatureProvider())).Value;
            }
        }

        private CustomAttribute Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetCustomAttribute(_handle)).Value;
            }
        }

        private CustomAttributeTypedArgument[] GetFixedArguments()
        {
            if (_fixedArguments != null)
            {
                return _fixedArguments.Copy();
            }

            var fixedArguments = new CustomAttributeTypedArgument[Value.FixedArguments.Length];
            for (var i = 0; i < fixedArguments.Length; i++)
            {
                fixedArguments[i] = new CustomAttributeTypedArgument(
                    Value.FixedArguments[i].Type,
                    Value.FixedArguments[i].Value);
            }

            _fixedArguments = fixedArguments;
            return _fixedArguments.Copy();
        }

        private CustomAttributeNamedArgument[] GetNamedArguments()
        {
            if (_namedArguments != null)
            {
                return _namedArguments.Copy();
            }

            var namedArguments = new CustomAttributeNamedArgument[Value.NamedArguments.Length];
            for (var i = 0; i < namedArguments.Length; i++)
            {
                MemberInfo member = Value.NamedArguments[i].Kind == CustomAttributeNamedArgumentKind.Field
                    ? (MemberInfo)Constructor.DeclaringType.GetField(Value.NamedArguments[i].Name)
                    : (MemberInfo)Constructor.DeclaringType.GetProperty(Value.NamedArguments[i].Name);

                namedArguments[i] = new CustomAttributeNamedArgument(
                    member,
                    new CustomAttributeTypedArgument(
                        Value.NamedArguments[i].Type,
                        Value.NamedArguments[i].Kind));
            }

            _namedArguments = namedArguments;
            return _namedArguments.Copy();
        }

        private ConstructorInfo GetConstructor()
        {
            if (_constructor != null)
            {
                return _constructor;
            }

            if (Definition.Constructor.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition definition = _metadata.GetMethodDefinition(
                    (MethodDefinitionHandle)Definition.Constructor);

                MetadataType type = MetadataDomain.GetMetadataType(
                    _metadata,
                    definition.GetDeclaringType());

                return type.GetMetadataConstructor(
                    MetadataTokens.GetToken(
                        _metadata,
                        Definition.Constructor));
            }

            MemberReference methodRef = _metadata.GetMemberReference(
                (MemberReferenceHandle)Definition.Constructor);

            MetadataType parent = MetadataDomain.GetMetadataType(_metadata, methodRef.Parent);
            var signature = methodRef.DecodeMethodSignature(
                new MetadataSignatureProvider(),
                new GenericContext()
                {
                    DeclaringType = parent
                });

            ParameterModifier[] parameterModifier =
                signature.ParameterTypes.Length == 0
                    ? Empty<ParameterModifier>.Array
                    : new[] { new ParameterModifier(signature.RequiredParameterCount) };

            return _constructor = parent.GetMetadataConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                default(CallingConventions),
                signature.ParameterTypes.ToArray(),
                parameterModifier);
        }
    }
}