using System;
using System.Globalization;

namespace MetadataAssembly
{
    internal class Error
    {
        internal static ArgumentException NewNotMetadataTypeException(string parameterName)
        {
            return new ArgumentException("Specified Type must be a MetadataType.", parameterName);
        }

        internal static ArgumentException NewInvalidMetadataTokenException(
            int metadataToken,
            string parameterName)
        {
            return new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "The metadata token '0x{0:x8}' is not valid for this member type.",
                    metadataToken));
        }

    }
}