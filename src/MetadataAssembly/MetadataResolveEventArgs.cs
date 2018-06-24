using System;
using System.Reflection;

namespace MetadataAssembly
{
    public class MetadataResolveEventArgs : EventArgs
    {
        internal MetadataResolveEventArgs(AssemblyName assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public AssemblyName AssemblyName { get; }

        public MetadataAssembly ResolvedAssembly { get; set; }
    }
}