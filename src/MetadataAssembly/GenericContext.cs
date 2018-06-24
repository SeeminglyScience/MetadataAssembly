namespace MetadataAssembly
{
    /// <summary>
    /// Provides generic parameter context for signature resolution via
    /// <see cref="MetadataSignatureProvider" />.
    /// </summary>
    internal class GenericContext
    {
        internal MetadataType DeclaringType { get; set; }

        internal MetadataMethodInfo DeclaringMethod { get; set; }
    }
}