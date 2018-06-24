using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MetadataAssembly
{
    internal class MetadataType : Type
    {
        private readonly MetadataAssembly _assembly;

        private readonly MetadataReader _metadata;

        private MetadataType _parent;

        private bool? _isNested;

        private string _name;

        private string _namespace;

        private TypeDefinition? _definition;

        private MetadataMethodInfo[] _methods;

        private MetadataConstructorInfo[] _constructors;

        private MetadataPropertyInfo[] _properties;

        private MetadataEventInfo[] _events;

        private MetadataFieldInfo[] _fields;

        private MetadataType[] _nestedTypes;

        private int[] _fieldTokens;

        private int[] _methodTokens;

        private int[] _eventTokens;

        private int[] _nestedTypeTokens;

        private int[] _constructorTokens;

        private int[] _propertyTokens;

        private MetadataType[] _genericParameters;

        private Type[] _interfaces;

        internal MetadataType(MetadataAssembly assembly, TypeDefinitionHandle handle)
        {
            _assembly = assembly;
            _metadata = assembly.Metadata;
            Handle = handle;
        }

        internal MetadataType(MetadataType parentType, TypeDefinitionHandle handle)
        {
            _isNested = true;
            _parent = parentType;
            _assembly = parentType._assembly;
            _metadata = parentType._assembly.Metadata;
            Handle = handle;
        }

        public override int MetadataToken => Handle.IsNil ? 0 : MetadataTokens.GetToken(_metadata, Handle);

        public override Assembly Assembly => _assembly;

        public override string AssemblyQualifiedName
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "{0}{1} {2}",
                    FullName,
                    CultureInfo.CurrentCulture.TextInfo.ListSeparator,
                    _assembly.GetName().ToString());
            }
        }

        public override Type BaseType => MetadataDomain.GetMetadataType(_assembly, Definition.BaseType);

        public override string FullName
        {
            get
            {
                if (IsNested)
                {
                    return string.Join("+", _parent.FullName, Name);
                }

                if (string.IsNullOrEmpty(Namespace))
                {
                    return Name;
                }

                return $"{Namespace}{Type.Delimiter}{Name}";
            }
        }

        public override Guid GUID => default(Guid);

        public override Module Module => Assembly.GetModules().First();

        public override Type DeclaringType
        {
            get
            {
                if (_isNested != null)
                {
                    return _parent;
                }

                var declaringTypeHandle = Definition.GetDeclaringType();
                if (declaringTypeHandle.IsNil)
                {
                    _isNested = false;
                    return null;
                }

                _isNested = true;
                return _parent = MetadataDomain.GetMetadataType(_assembly, declaringTypeHandle);
            }
        }

        public override string Namespace
        {
            get
            {
                if (IsNested)
                {
                    return _parent.Namespace;
                }

                if (!string.IsNullOrEmpty(_namespace))
                {
                    return _namespace;
                }

                return _namespace = _metadata.GetString(Definition.Namespace);
            }
        }

        public override Type UnderlyingSystemType => this;

        public override string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(_name))
                {
                    return _name;
                }

                return _name = _metadata.GetString(Definition.Name);
            }
        }

        public override bool IsEnum =>
            BaseType == MetadataDomain.GetMetadataType(typeof(Enum));

        protected internal TypeDefinitionHandle Handle { get; }

        private TypeDefinition Definition
        {
            get
            {
                if (_definition != null)
                {
                    return _definition.Value;
                }

                return (_definition = _metadata.GetTypeDefinition(Handle)).Value;
            }
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            MaybeLoadMethods();
            return _constructors.Where(constructor => constructor.DoesMatchFlags(bindingAttr)).ToArray();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override Type GetElementType()
        {
            return null;
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            return GetMetadataEvent(name, bindingAttr);
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            MaybeLoadEvents();
            return _events.Where(eventInfo => eventInfo.DoesMatchFlags(bindingAttr)).ToArray();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return GetMetadataField(name, bindingAttr);
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            MaybeLoadFields();
            return _fields.Where(field => field.DoesMatchFlags(bindingAttr)).ToArray();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return GetInterfaces()
                .FirstOrDefault(
                    i => name.Equals(i.FullName, comparison) || name.Equals(i.Name, comparison));
        }

        public override Type[] GetInterfaces()
        {
            if (_interfaces != null)
            {
                return _interfaces;
            }

            return _interfaces = GetInterfacesImpl();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            MaybeLoadEvents();
            MaybeLoadFields();
            MaybeLoadMethods();
            MaybeLoadProperties();
            return _events.Where(eventInfo => eventInfo.DoesMatchFlags(bindingAttr))
                .Cast<MemberInfo>()
                .Concat(_fields.Where(field => field.DoesMatchFlags(bindingAttr)))
                .Concat(_methods.Where(method => method.DoesMatchFlags(bindingAttr)))
                .Concat(_constructors.Where(constructor => constructor.DoesMatchFlags(bindingAttr)))
                .Concat(_properties.Where(property => property.DoesMatchFlags(bindingAttr)))
                .ToArray();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            MaybeLoadMethods();
            return _methods.Where(method => method.DoesMatchFlags(bindingAttr)).ToArray();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            return GetNestedMetadataType(name, bindingAttr);
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            MaybeLoadTypes();
            return _nestedTypes.Where(type => type.DoesMatchFlags(bindingAttr)).ToArray();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            MaybeLoadProperties();
            return _properties.Where(property => property.DoesMatchFlags(bindingAttr)).ToArray();
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override Type GetEnumUnderlyingType()
        {
            if (!IsEnum)
            {
                throw new ArgumentException(
                    "Type provided must be an Enum.",
                    "enumType");
            }

            return GetField(
                "value__",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FieldType;
        }

        public override string[] GetEnumNames()
        {
            if (!IsEnum)
            {
                throw new ArgumentException(
                    "Type provided must be an Enum.",
                    "enumType");
            }

            return GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field.Name)
                .ToArray();
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return Definition.Attributes;
        }

        protected override ConstructorInfo GetConstructorImpl(
            BindingFlags bindingAttr,
            Binder binder,
            CallingConventions callConvention,
            Type[] types,
            ParameterModifier[] modifiers)
        {
            if (!Util.TryGetAsMetadataTypes(types, out MetadataType[] metadataTypes))
            {
                throw NewNotMetadataTypeException(nameof(types));
            }

            return GetMetadataConstructor(
                bindingAttr,
                binder,
                callConvention,
                metadataTypes,
                modifiers);
        }

        protected override MethodInfo GetMethodImpl(
            string name,
            BindingFlags bindingAttr,
            Binder binder,
            CallingConventions callConvention,
            Type[] types,
            ParameterModifier[] modifiers)
        {
            if (!Util.TryGetAsMetadataTypes(types, out MetadataType[] metadataTypes))
            {
                throw NewNotMetadataTypeException(nameof(types));
            }

            return GetMetadataMethod(
                name,
                bindingAttr,
                binder,
                callConvention,
                metadataTypes,
                modifiers);
        }

        protected override PropertyInfo GetPropertyImpl(
            string name,
            BindingFlags bindingAttr,
            Binder binder,
            Type returnType,
            Type[] types,
            ParameterModifier[] modifiers)
        {
            if (returnType != null && !(returnType is MetadataType))
            {
                throw NewNotMetadataTypeException(nameof(returnType));
            }

            if (!Util.TryGetAsMetadataTypes(types, out MetadataType[] metadataTypes))
            {
                throw NewNotMetadataTypeException(nameof(types));
            }

            return GetMetadataProperty(
                name,
                bindingAttr,
                binder,
                (MetadataType)returnType,
                metadataTypes,
                modifiers);
        }

        protected override bool HasElementTypeImpl() => false;

        protected override bool IsArrayImpl() => false;

        protected override bool IsByRefImpl() => false;

        protected override bool IsCOMObjectImpl() => false;

        protected override bool IsPointerImpl() => false;

        protected override bool IsPrimitiveImpl()
        {
            return Util.TryGetPrimitiveTypeCode(this, out _);
        }

        protected override bool IsValueTypeImpl()
        {
            return !IsEnum
                && MetadataDomain
                    .GetMetadataType(typeof(System.ValueType))
                    .IsAssignableFrom(this);
        }

        internal MetadataAssembly GetMetadataAssembly()
        {
            return _assembly;
        }

        internal MetadataType GetNestedMetadataType(string name, BindingFlags bindingAttr = Util.PublicFlags)
        {
            MaybeLoadTypes();
            return _nestedTypes.FirstOrDefault(type => type.IsMatch(name, bindingAttr));
        }

        internal MetadataEventInfo GetMetadataEvent(string name, BindingFlags bindingAttr = Util.PublicFlags)
        {
            MaybeLoadEvents();
            return _events.FirstOrDefault(eventInfo => eventInfo.IsMatch(name, bindingAttr));
        }


        internal MetadataFieldInfo GetMetadataField(string name, BindingFlags bindingAttr = Util.PublicFlags)
        {
            MaybeLoadFields();
            return _fields.FirstOrDefault(field => field.IsMatch(name, bindingAttr));
        }

        internal MetadataConstructorInfo GetMetadataConstructor(
            BindingFlags bindingAttr = Util.PublicFlags,
            Binder binder = null,
            CallingConventions callConvention = default(CallingConventions),
            MetadataType[] types = null,
            ParameterModifier[] modifiers = null)
        {
            MaybeLoadMethods();
            return _constructors
                .FirstOrDefault(constructor => constructor.IsMatchFull(types, callConvention, bindingAttr));
        }

        internal MetadataMethodInfo GetMetadataMethod(
            string name,
            BindingFlags bindingAttr = Util.PublicFlags,
            Binder binder = null,
            CallingConventions callConvention = default(CallingConventions),
            MetadataType[] types = null,
            ParameterModifier[] modifiers = null)
        {
            MaybeLoadMethods();
            return _methods
                .FirstOrDefault(method => method.IsMatchFull(name, types, callConvention, bindingAttr));
        }

        internal MetadataPropertyInfo GetMetadataProperty(
            string name,
            BindingFlags bindingAttr = Util.PublicFlags,
            Binder binder = null,
            MetadataType returnType = null,
            MetadataType[] types = null,
            ParameterModifier[] modifiers = null)
        {
            MaybeLoadProperties();
            return _properties
                .FirstOrDefault(property => property.IsMatchFull(name, returnType, types, bindingAttr));
        }

        internal MetadataFieldInfo GetMetadataField(int metadataToken)
        {
            MaybeLoadFields(shouldCacheTokens: true);
            var index = Array.IndexOf(_fieldTokens, metadataToken);
            return index == -1 ? null : _fields[index];
        }

        internal MetadataMethodInfo GetMetadataMethod(int metadataToken)
        {
            MaybeLoadMethods(shouldCacheTokens: true);
            var index = Array.IndexOf(_methodTokens, metadataToken);
            return index == -1 ? null : _methods[index];
        }

        internal MetadataConstructorInfo GetMetadataConstructor(int metadataToken)
        {
            MaybeLoadMethods(shouldCacheTokens: true);
            var index = Array.IndexOf(_constructorTokens, metadataToken);
            return index == -1 ? null : _constructors[index];
        }

        internal MetadataPropertyInfo GetMetadataProperty(int metadataToken)
        {
            MaybeLoadProperties(shouldCacheTokens: true);
            var index = Array.IndexOf(_propertyTokens, metadataToken);
            return index == -1 ? null : _properties[index];
        }

        internal MetadataEventInfo GetMetadataEvent(int metadataToken)
        {
            MaybeLoadEvents(shouldCacheTokens: true);
            var index = Array.IndexOf(_eventTokens, metadataToken);
            return index == -1 ? null : _events[index];
        }

        internal MetadataType GetNestedMetadataType(int metadataToken)
        {
            MaybeLoadTypes(shouldCacheTokens: true);
            var index = Array.IndexOf(_nestedTypeTokens, metadataToken);
            return index == -1 ? null : _nestedTypes[index];
        }

        private void MaybeLoadEvents(bool shouldCacheTokens = false)
        {
            if (_events != null)
            {
                if (!shouldCacheTokens)
                {
                    return;
                }

                MaybeCacheTokens(_events, ref _eventTokens);
                return;
            }

            if (Handle.IsNil)
            {
                _events = Empty<MetadataEventInfo>.Array;
                return;
            }

            var events = new MetadataEventInfo[Definition.GetEvents().Count];
            if (events.Length == 0)
            {
                _eventTokens = Empty<int>.Array;
                _events = events;
                return;
            }

            int firstRowNumber = MetadataTokens.GetRowNumber(Definition.GetEvents().First());
            for (var i = 0; i < events.Length; i++)
            {
                events[i] = new MetadataEventInfo(
                    this,
                    MetadataTokens.EventDefinitionHandle(i + firstRowNumber));
            }

            _events = events;
            if (shouldCacheTokens)
            {
                MaybeCacheTokens(_events, ref _eventTokens);
            }
        }

        private void MaybeLoadFields(bool shouldCacheTokens = false)
        {
            if (_fields != null)
            {
                if (!shouldCacheTokens)
                {
                    return;
                }

                MaybeCacheTokens(_fields, ref _fieldTokens);
                return;
            }

            var fields = new MetadataFieldInfo[Definition.GetFields().Count];
            if (fields.Length == 0)
            {
                _fields = fields;
                _fieldTokens = Empty<int>.Array;
                return;
            }

            int firstRowNumber = MetadataTokens.GetRowNumber(Definition.GetFields().First());

            for (var i = 0; i < fields.Length; i++)
            {
                fields[i] = new MetadataFieldInfo(
                    this,
                    MetadataTokens.FieldDefinitionHandle(i + firstRowNumber));
            }

            _fields = fields;
            if (shouldCacheTokens)
            {
                MaybeCacheTokens(_fields, ref _fieldTokens);
            }
        }

        private void MaybeLoadProperties(bool shouldCacheTokens = false)
        {
            if (_properties != null)
            {
                if (!shouldCacheTokens)
                {
                    return;
                }

                MaybeCacheTokens(_properties, ref _propertyTokens);
                return;
            }

            var properties = new MetadataPropertyInfo[Definition.GetProperties().Count];
            if (properties.Length == 0)
            {
                _properties = properties;
                _propertyTokens = Empty<int>.Array;
                return;
            }

            int firstRowNumber = MetadataTokens.GetRowNumber(Definition.GetProperties().First());
            for (var i = 0; i < properties.Length; i++)
            {
                properties[i] = new MetadataPropertyInfo(
                    this,
                    MetadataTokens.PropertyDefinitionHandle(i + firstRowNumber));
            }

            _properties = properties;
            if (shouldCacheTokens)
            {
                MaybeCacheTokens(_properties, ref _propertyTokens);
            }
        }

        private void MaybeLoadTypes(bool shouldCacheTokens = false)
        {
            if (_nestedTypes != null)
            {
                if (!shouldCacheTokens)
                {
                    return;
                }

                MaybeCacheTokens(_nestedTypes, ref _nestedTypeTokens);
                return;
            }

            if (Handle.IsNil)
            {
                _nestedTypes = Empty<MetadataType>.Array;
                return;
            }

            var typeHandles = Definition.GetNestedTypes();
            var types = new MetadataType[typeHandles.Length];
            var i = 0;
            foreach (TypeDefinitionHandle typeHandle in typeHandles)
            {
                types[i] = new MetadataType(this, typeHandle);
                i++;
            }

            _nestedTypes = types;
            if (shouldCacheTokens)
            {
                MaybeCacheTokens(_nestedTypes, ref _nestedTypeTokens);
            }
        }

        private void MaybeCacheTokens(MemberInfo[] members, ref int[] tokens)
        {
            if (tokens != null)
            {
                return;
            }

            tokens = new int[members.Length];
            for (var i = 0; i < members.Length; i++)
            {
                tokens[i] = members[i].MetadataToken;
            }
        }

        private void MaybeLoadMethods(bool shouldCacheTokens = false)
        {
            if (_methods != null || _constructors != null)
            {
                if (!shouldCacheTokens)
                {
                    return;
                }

                MaybeCacheTokens(_constructors, ref _constructorTokens);
                MaybeCacheTokens(_methods, ref _methodTokens);
            }

            if (Handle.IsNil)
            {
                _constructors = Empty<MetadataConstructorInfo>.Array;
                _methods = Empty<MetadataMethodInfo>.Array;
                return;
            }

            var methods = new List<MetadataMethodInfo>();
            var constructors = new List<MetadataConstructorInfo>();

            foreach (var method in Definition.GetMethods())
            {
                var methodDef = _metadata.GetMethodDefinition(method);
                var methodName = _metadata.GetString(methodDef.Name);
                if (methodName.Equals(".ctor", StringComparison.Ordinal) ||
                    methodName.Equals(".cctor", StringComparison.Ordinal))
                {
                    constructors.Add(
                        new MetadataConstructorInfo(
                            this,
                            method,
                            methodDef,
                            methodName));

                    continue;
                }

                methods.Add(
                    new MetadataMethodInfo(
                        this,
                        method,
                        methodDef,
                        methodName));
            }

            _constructors = constructors.ToArray();
            _methods = methods.ToArray();

            if (shouldCacheTokens)
            {
                MaybeCacheTokens(_constructors, ref _constructorTokens);
                MaybeCacheTokens(_methods, ref _methodTokens);
            }
        }

        public override Type MakeByRefType()
        {
            return new MetadataByRefType(this);
        }

        public override Type MakeArrayType()
        {
            return new MetadataArrayType(this, 1);
        }

        public override Type MakeArrayType(int rank)
        {
            return new MetadataArrayType(this, rank);
        }

        public override Type MakePointerType()
        {
            return new MetadataPointerType(this);
        }

        public override bool IsGenericType => Definition.GetGenericParameters().Count != 0;

        public override bool IsGenericTypeDefinition => IsGenericType;
        
        public override bool IsConstructedGenericType => false;

        public override Type[] GetGenericArguments()
        {
            if (_genericParameters != null)
            {
                return _genericParameters.Copy();
            }

            _genericParameters =
                Definition.GetGenericParameters()
                    .Select(handle => new MetadataGenericParameterType(_assembly, handle, this))
                    .ToArray();

            return _genericParameters.Copy();
        }

        public override Type MakeGenericType(params Type[] typeArguments)
        {
            if (!IsGenericType)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "{0} is not a GenericTypeDefinition. MakeGenericType may only be called on a type for which Type.IsGenericTypeDefinition is true.",
                        Name));
            }

            Type[] parameters = GetGenericArguments();
            if (typeArguments == null || typeArguments.Length != parameters.Length)
            {
                throw new ArgumentException(
                    "The number of generic arguments provided doesn't equal the arity of the generic type definition.",
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

            return new MetadataGenericConstructedType(_assembly, this, instantiation);
        }

        public override string ToString()
        {
            if (!IsGenericType)
            {
                return FullName;
            }

            var result = new StringBuilder(FullName).Append('[');
            foreach (var typeArg in GetGenericArguments())
            {
                result.Append(typeArg.ToString()).Append(',');
            }

            return result
                .Remove(result.Length - 1, 1)
                .Append(']')
                .ToString();
        }

        public override System.Collections.Generic.IList<CustomAttributeData> GetCustomAttributesData()
        {
            return Definition
                .GetCustomAttributes()
                .Select(handle => new MetadataCustomAttributeData(_metadata, handle))
                .ToArray();
        }

        protected virtual Type[] GetInterfacesImpl()
        {
            if (Handle.IsNil)
            {
                return Empty<Type>.Array;
            }

            return Definition.GetInterfaceImplementations()
                .Select(
                    handle => MetadataDomain.GetMetadataType(
                        _metadata,
                        _metadata.GetInterfaceImplementation(handle).Interface))
                .ToArray();
        }

        private ArgumentException NewNotMetadataTypeException(string parameterName)
        {
            return new ArgumentException("Specified Type must be a MetadataType.", parameterName);
        }
    }
}