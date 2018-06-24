using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace MetadataAssembly
{
    public class MetadataDomain
    {
        private static readonly Dictionary<string, MetadataAssembly> s_assemblies = new Dictionary<string, MetadataAssembly>();

        private static int s_resolutionType = (int)AssemblyResolutionType.Manual;

        private MetadataDomain()
        {
        }

        public static event EventHandler<MetadataResolveEventArgs> MetadataAssemblyResolve;

        public static AssemblyResolutionType ResolutionType => (AssemblyResolutionType)s_resolutionType;

        public static MetadataAssembly LoadMetadataAssembly(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            path = Path.GetFullPath(path);

            MetadataAssembly assembly;
            if (s_assemblies.TryGetValue(path, out assembly))
            {
                return assembly;
            }

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var peReader = new PEReader(stream);
            MetadataReader metadata = peReader.GetMetadataReader();
            assembly = new MetadataAssembly(peReader, metadata, path);
            s_assemblies.Add(path, assembly);

            return assembly;
        }

        public static MetadataAssembly GetMetadataAssembly(AssemblyName assemblyName)
        {
            return s_assemblies.Values
                .FirstOrDefault(assembly => Util.IsAssemblyNameMatch(assemblyName, assembly.GetName()));
        }

        public static void SetAssemblyResolution(AssemblyResolutionType resolutionType)
        {
            Interlocked.Exchange(ref s_resolutionType, (int)resolutionType);
        }

        internal static MetadataType GetMetadataType(string fullName)
        {
            foreach (var assembly in s_assemblies.Values)
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return (MetadataType)type;
                }
            }

            return null;
        }

        internal static MetadataType GetMetadataType(Type type)
        {
            var foundAssembly = ResolveAssembly(type.Assembly.GetName());
            return foundAssembly.GetType(type.MetadataToken);
        }

        internal static MetadataType GetMetadataType(MetadataReader metadata, EntityHandle handle)
        {
            return GetMetadataType(GetAssemblyByMetadata(metadata), handle);
        }

        internal static MetadataType GetMetadataType(MetadataAssembly assembly, EntityHandle handle)
        {
            if (handle.Kind == HandleKind.TypeDefinition)
            {
                return assembly.GetType((TypeDefinitionHandle)handle);
            }

            if (handle.Kind != HandleKind.TypeReference)
            {
                throw new ArgumentException("Invalid handle kind.", nameof(handle));
            }

            TypeReference typeRef = assembly.Metadata.GetTypeReference((TypeReferenceHandle)handle);
            string @namespace = assembly.Metadata.GetString(typeRef.Namespace);
            string name = assembly.Metadata.GetString(typeRef.Name);
            string fullName = string.IsNullOrEmpty(@namespace)
                ? name
                : string.Join(Type.Delimiter.ToString(), @namespace, name);

            MetadataType resolvedType = null;
            MetadataAssembly resolvedAssembly = null;
            if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                var parent = GetMetadataType(assembly, typeRef.ResolutionScope);
                resolvedType = parent.GetNestedMetadataType(fullName, Util.AllFlags);
                resolvedAssembly = resolvedType.GetMetadataAssembly();
            }
            else if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                AssemblyReference assemblyRef =
                    assembly.Metadata.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);

                resolvedAssembly = ResolveAssembly(assembly.Metadata, assemblyRef);
                resolvedType = resolvedAssembly.GetType(fullName) as MetadataType;
            }

            if (resolvedType != null)
            {
                return resolvedType;
            }

            // Try to find the type in the core framework library as a fallback. I couldn't quickly
            // find a way to determine which assembly a type would be forwarded to programmatically.
            if (!fullName.StartsWith("System.", StringComparison.Ordinal))
            {
                return null;
            }

            resolvedAssembly = ResolveAssembly(typeof(object).Assembly.GetName());
            return resolvedAssembly.GetType(fullName) as MetadataType;
        }

        internal static MetadataAssembly GetAssemblyByMetadata(MetadataReader metadata)
        {
            return s_assemblies.Values
                .FirstOrDefault(assembly => assembly.Metadata == metadata);
        }

        internal static MetadataAssembly ResolveAssembly(MetadataReader metadata, AssemblyReference assemblyRef)
        {
            AssemblyName assemblyName = Util.GetAssemblyName(metadata, assemblyRef);
            return ResolveAssembly(assemblyName);
        }

        internal static MetadataAssembly ResolveAssembly(AssemblyName assemblyName)
        {
            // Hacky workaround for shims.
            if (assemblyName.Name == "System.Runtime" || assemblyName.Name == "netstandard")
            {
                assemblyName = typeof(object).Assembly.GetName();
            }

            MetadataAssembly resolvedAssembly = GetMetadataAssembly(assemblyName);
            if (resolvedAssembly != null)
            {
                return resolvedAssembly;
            }

            if (ResolutionType == AssemblyResolutionType.CurrentAppDomain)
            {
                Assembly foundAssembly = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(
                        assembly => Util.IsAssemblyNameMatch(assemblyName, assembly.GetName()));

                if (foundAssembly != null && !string.IsNullOrEmpty(foundAssembly.Location))
                {
                    return LoadMetadataAssembly(foundAssembly.Location);
                }
            }

            var eventArgs = new MetadataResolveEventArgs(assemblyName);
            MetadataAssemblyResolve?.Invoke(null, eventArgs);
            if (eventArgs.ResolvedAssembly != null)
            {
                return eventArgs.ResolvedAssembly;
            }

            throw new InvalidOperationException($"Cannot resolve assembly {assemblyName.ToString()}. You can enable automatic metadata assembly resolution using the method MetadataDomain.SetAssemblyResolution.");
        }
    }
}