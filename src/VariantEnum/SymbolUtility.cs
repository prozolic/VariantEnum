using Microsoft.CodeAnalysis;

namespace VariantEnum;

internal static class SymbolUtility
{
    public static IEnumerable<AttributeData> GetAttribute(this ISymbol symbol, string namespaceName, string attributeName)
        => symbol.GetAttributes().Where(a =>
            a.AttributeClass!.ContainingNamespace.Name == namespaceName &&
            a.AttributeClass!.Name == attributeName);
}
