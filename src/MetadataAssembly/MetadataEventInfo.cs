using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace MetadataAssembly
{
    internal class MetadataEventInfo : EventInfo
    {
        private readonly MetadataType _type;

        private readonly MetadataReader _metadata;

        private readonly EventDefinitionHandle _handle;

        private EventDefinition? _definition;

        internal MetadataEventInfo(MetadataType type, EventDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;
            _metadata = ((MetadataAssembly)type.Assembly).Metadata;
        }

        public override EventAttributes Attributes => Definition.Attributes;

        public override Type DeclaringType => _type;

        public override string Name => _metadata.GetString(Definition.Name);

        public override Type ReflectedType => _type;

        public override Type EventHandlerType =>
            GetAddMethod(nonPublic: true)
                .GetParameters()
                .FirstOrDefault()
                ?.ParameterType;

        private EventDefinition Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetEventDefinition(_handle)).Value;
            }
        }

        public override MethodInfo GetAddMethod(bool nonPublic)
        {
            return GetMethodFromParent(Definition.GetAccessors().Adder, nonPublic);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo GetRaiseMethod(bool nonPublic)
        {
            return GetMethodFromParent(Definition.GetAccessors().Raiser, nonPublic);
        }

        public override MethodInfo GetRemoveMethod(bool nonPublic)
        {
            return GetMethodFromParent(Definition.GetAccessors().Remover, nonPublic);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
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
            return new System.Text.StringBuilder(EventHandlerType.ToString())
                .Append(' ')
                .Append(Name)
                .ToString();
        }

        private MethodInfo GetMethodFromParent(MethodDefinitionHandle methodHandle, bool isNonPublic)
        {
            var methodDef = _metadata.GetMethodDefinition(methodHandle);
            var flags = BindingFlags.Instance | BindingFlags.Static;
            flags |= isNonPublic ? BindingFlags.NonPublic | BindingFlags.Public : BindingFlags.Public;

            return _type.GetMetadataMethod(
                _metadata.GetString(methodDef.Name),
                flags);
        }
    }
}