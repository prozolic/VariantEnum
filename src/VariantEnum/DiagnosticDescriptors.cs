using Microsoft.CodeAnalysis;

namespace VariantEnum;

internal static class DiagnosticDescriptors
{
    const string Category = "VariantEnumError";

    public static readonly DiagnosticDescriptor MustNotBeNestedType = new(
        id: "VariantEnumError001",
        title: "Variant enum type must not be nested type",
        messageFormat: "Variant enum type must not be nested type: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

}
