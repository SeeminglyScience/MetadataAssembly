namespace MetadataAssembly
{
    internal enum TokenKind
    {
        Module = 0x00,

        TypeReference = 0x01,

        TypeDefinition = 0x02,

        FieldDefinition = 0x04,

        MethodDefinition = 0x06,

        ParamDefinition = 0x08,

        InterfaceImplementation = 0x09,

        MemberReference = 0x0a,

        CustomAttribute = 0x0c,

        Permission = 0x0e,

        Signature = 0x11,

        Event = 0x14,

        Property = 0x17,

        ModuleReference = 0x1a,

        TypeSpec = 0x1b,

        Assembly = 0x20,

        AssemblyReference = 0x23,

        File = 0x26,

        ExportedType = 0x27,

        ManifestResource = 0x28,

        GenericParam = 0x2a,

        MethodSpec = 0x2b,

        GenericParamConstraint = 0x2c,

        String = 0x70,

        Name = 0x71,

        BaseType = 0x72
    }
}