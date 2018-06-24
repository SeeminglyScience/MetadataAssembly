namespace MetadataAssembly
{
    /// <summary>
    /// Represents the type of automatic resolution that will be used when attempting to
    /// resolve a <see cref="MetadataAssembly" />.
    /// </summary>
    public enum AssemblyResolutionType
    {
        Manual,

        CurrentAppDomain
    }
}