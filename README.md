# Metadata Assembly

A reflection only implementation of the common `System.Reflection` types using API's from the
`System.Reflection.Metadata` namespace.

This project serves mainly as an example.  This may be particularly useful for those who are already
familar with the existing API's but may have issues translating that to the metadata API's.

## Usage (PowerShell)

### Clone and build the repo

```powershell
git clone https://github.com/SeeminglyScience/MetadataAssembly
cd ./MetadataAssembly
dotnet build
Add-Type -Path ./src/MetadataAssembly/bin/Debug/netstandard2.0/MetadataAssembly.dll
```

### Load an assembly

```powershell
# Optional. This enables automatic loading of referenced assemblies as metadata assemblies.
# You can also load them manually, or use the MetadataDomain.MetadataAssemblyResolve event.
[MetadataAssembly.MetadataDomain]::SetAssemblyResolution('CurrentAppDomain')

# Load SMA
$assembly = [MetadataAssembly.MetadataDomain]::LoadMetadataAssembly([psobject].Assembly.Location)
$assembly.GetTypes()
```