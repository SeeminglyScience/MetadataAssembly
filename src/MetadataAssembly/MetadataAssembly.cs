using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace MetadataAssembly
{
    /// <summary>
    /// Represents a reflection only <see cref="Assembly" /> implemented
    /// with <see cref="MetadataReader" />.
    /// </summary>
    public class MetadataAssembly : Assembly
    {
        private readonly AssemblyDefinition _assembly;

        private readonly string _path;

        private readonly PEReader _peReader;

        private MetadataType[] _types;

        private int[] _tokens;

        private AssemblyName _name;

        private MetadataModule _module;

        public MetadataAssembly(PEReader peReader, MetadataReader metadata, string path)
        {
            Metadata = metadata;
            _assembly = metadata.GetAssemblyDefinition();
            _path = path;
            _peReader = peReader;
        }

        public override string Location => _path;

        public override bool ReflectionOnly => true;

        public override bool IsDynamic => false;

        public override string ImageRuntimeVersion => Metadata.MetadataVersion;

        internal MetadataReader Metadata { get; }

        public override Module GetModule(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            return GetModules(getResourceModules: false)
                .FirstOrDefault(module => name.Equals(module.Name, StringComparison.Ordinal));
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            if (_module != null)
            {
                return new[] { _module };
            }

            _module = new MetadataModule(this, Metadata.GetModuleDefinition());
            return new[] { _module };
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return GetModules(getResourceModules);
        }

        public override AssemblyName GetName()
        {
            if (_name != null)
            {
                return _name;
            }

            var assemblyName = new AssemblyName(Metadata.GetString(_assembly.Name));
            if ((_assembly.Flags & AssemblyFlags.PublicKey) != 0)
            {
                assemblyName.SetPublicKey(Metadata.GetBlobBytes(_assembly.PublicKey));
            }
            else
            {
                assemblyName.SetPublicKeyToken(Metadata.GetBlobBytes(_assembly.PublicKey));
            }

            assemblyName.CultureName = Metadata.GetString(_assembly.Culture);
            assemblyName.Version = _assembly.Version;
            return _name = assemblyName;
        }

        public override Type[] GetTypes()
        {
            MaybeLoadTypes();
            return _types.Copy();
        }

        public override Type GetType(string name)
        {
            return GetTypeImpl(name);
        }

        public override Type GetType(string name, bool throwOnError)
        {
            return GetTypeImpl(name, throwOnError);
        }

        public override Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            return GetTypeImpl(name, throwOnError, ignoreCase);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return _assembly
                .GetCustomAttributes()
                .Select(handle => new MetadataCustomAttributeData(Metadata, handle))
                .ToArray();
        }

        public override AssemblyName[] GetReferencedAssemblies()
        {
            return Metadata.AssemblyReferences
                .Select(handle => Util.GetAssemblyName(Metadata, Metadata.GetAssemblyReference(handle)))
                .ToArray();
        }

        public override Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return GetName().ToString();
        }

        internal MetadataType GetType(TypeDefinitionHandle handle)
        {
            return GetType(MetadataTokens.GetToken(Metadata, handle));
        }

        internal MetadataType GetType(int metadataToken)
        {
            MaybeLoadTypes();
            int index = Array.IndexOf(_tokens, metadataToken);
            if (index == -1)
            {
                return null;
            }

            return _types[index];
        }

        private MetadataType GetTypeImpl(string name, bool throwOnError = false, bool ignoreCase = false)
        {
            MaybeLoadTypes();
            var comparision = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            for (var i = 0; i < _types.Length; i++)
            {
                if (_types[i].FullName.Equals(name, comparision))
                {
                    return _types[i];
                }
            }

            if (throwOnError)
            {
                throw new TypeLoadException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Could not load type '{0}' from assembly '{1}'.",
                        name,
                        GetName()));
            }

            return null;
        }

        private void MaybeLoadTypes()
        {
            if (_types != null)
            {
                return;
            }

            var types = new MetadataType[Metadata.TypeDefinitions.Count];
            var tokens = new int[Metadata.TypeDefinitions.Count];
            for (var i = 0; i < types.Length; i++)
            {
                types[i] = new MetadataType(this, MetadataTokens.TypeDefinitionHandle(i + 1));
                tokens[i] = types[i].MetadataToken;
            }

            _types = types;
            _tokens = tokens;
        }
    }
}
