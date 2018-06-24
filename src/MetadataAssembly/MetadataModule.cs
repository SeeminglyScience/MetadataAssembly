using System;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace MetadataAssembly
{
    internal class MetadataModule : Module
    {
        private readonly MetadataReader _metadata;

        private readonly MetadataAssembly _assembly;

        private readonly ModuleDefinition _definition;

        internal MetadataModule(MetadataAssembly assembly, ModuleDefinition definition)
        {
            _assembly = assembly;
            _metadata = assembly.Metadata;
            _definition = definition;
        }

        public override Assembly Assembly => _assembly;

        public override Guid ModuleVersionId => _metadata.GetGuid(_definition.Mvid);

        public override string Name => _metadata.GetString(_definition.Name);

        public override string ScopeName => Name;

        public override string FullyQualifiedName =>
            System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Assembly.Location),
                Name);

        public override int MetadataToken => 1;

        public override FieldInfo ResolveField(
            int metadataToken,
            Type[] genericTypeArguments,
            Type[] genericMethodArguments)
        {
            return ResolveFieldImpl(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override Type ResolveType(
            int metadataToken,
            Type[] genericTypeArguments,
            Type[] genericMethodArguments)
        {
            return ResolveTypeImpl(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override MethodBase ResolveMethod(
            int metadataToken,
            Type[] genericTypeArguments,
            Type[] genericMethodArguments)
        {
            return ResolveMethodImpl(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override MemberInfo ResolveMember(
            int metadataToken,
            Type[] genericTypeArguments,
            Type[] genericMethodArguments)
        {
            MemberInfo member;
            member = ResolveMethodImpl(
                metadataToken,
                genericTypeArguments,
                genericMethodArguments,
                shouldThrow: false);

            if (member != null)
            {
                return member;
            }

            member = ResolveFieldImpl(
                metadataToken,
                genericTypeArguments,
                genericMethodArguments,
                shouldThrow: false);

            if (member != null)
            {
                return member;
            }

            member = ResolveTypeImpl(
                metadataToken,
                genericTypeArguments,
                genericMethodArguments,
                shouldThrow: false);

            if (member != null)
            {
                return member;
            }

            throw NewTokenOutOfRangeException(metadataToken, nameof(metadataToken));
        }

        private MetadataFieldInfo ResolveFieldImpl(
            int metadataToken,
            Type[] genericTypeArguments,
            Type[] genericMethodArguments,
            bool shouldThrow = true)
        {
            var entity = MetadataTokens.EntityHandle(metadataToken);
            if (entity.IsNil || entity.Kind != HandleKind.FieldDefinition)
            {
                if (!shouldThrow)
                {
                    return null;
                }

                throw NewTokenOutOfRangeException(metadataToken, nameof(metadataToken));
            }

            var field = _metadata.GetFieldDefinition((FieldDefinitionHandle)entity);
            var type = ResolveTypeImpl(
                MetadataTokens.GetToken(_metadata, field.GetDeclaringType()),
                genericTypeArguments,
                genericMethodArguments);

            return type.GetMetadataField(MetadataTokens.GetToken(_metadata, entity));
        }

        private MetadataType ResolveTypeImpl(
            int metadataToken,
            Type[] genericTypeArguments,
            Type[] genericMethodArguments,
            bool shouldThrow = true)
        {
            MetadataType type = genericTypeArguments == null || genericTypeArguments.Length == 0
                ? _assembly.GetType(metadataToken)
                : (MetadataType)_assembly.GetType(metadataToken).MakeGenericType(genericTypeArguments);

            if (type != null || !shouldThrow)
            {
                return type;
            }

            throw NewTokenOutOfRangeException(metadataToken, nameof(metadataToken));
        }

        private MethodBase ResolveMethodImpl(
            int metadataToken,
            Type[] genericTypeArguments,
            Type[] genericMethodArguments,
            bool shouldThrow = true)
        {
            var entity = MetadataTokens.EntityHandle(metadataToken);
            if (entity.IsNil || entity.Kind != HandleKind.MethodDefinition)
            {
                if (!shouldThrow)
                {
                    return null;
                }

                throw NewTokenOutOfRangeException(metadataToken, nameof(metadataToken));
            }

            var method = _metadata.GetMethodDefinition((MethodDefinitionHandle)entity);
            var type = ResolveTypeImpl(
                MetadataTokens.GetToken(_metadata, method.GetDeclaringType()),
                genericTypeArguments,
                genericMethodArguments);

            var methodInfo = type.GetMetadataMethod(MetadataTokens.GetToken(_metadata, entity));
            if (methodInfo != null)
            {
                if (genericMethodArguments == null || genericMethodArguments.Length == 0)
                {
                    return methodInfo;
                }

                return methodInfo.MakeGenericMethod(genericMethodArguments);
            }

            return type.GetMetadataConstructor(MetadataTokens.GetToken(_metadata, entity));
        }

        private ArgumentOutOfRangeException NewTokenOutOfRangeException(int metadataToken, string parameterName)
        {
            return new ArgumentOutOfRangeException(
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    "Token 0x{0:x8} is not valid in the scope of module {1}.",
                    metadataToken,
                    Name),
                parameterName);
        }
    }
}