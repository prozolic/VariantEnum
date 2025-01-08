using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VariantEnum;

internal static class TypeDeclarationSyntaxExtensions
{
    public static bool IsPartial(this EnumDeclarationSyntax target)
        => target.AnyModifiers(syntaxKind: Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword);

    public static bool AnyModifiers(this EnumDeclarationSyntax target, Microsoft.CodeAnalysis.CSharp.SyntaxKind syntaxKind)
        => target.Modifiers.Any(m => m.IsKind(syntaxKind));

    public static bool IsNested(this EnumDeclarationSyntax target)
        => target.Parent is TypeDeclarationSyntax;

}
