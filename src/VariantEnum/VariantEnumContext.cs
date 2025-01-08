using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace VariantEnum;

internal class VariantEnumContext(
    EnumDeclarationSyntax enumDeclarationSyntax,
    Compilation compilation,
    ImmutableArray<EnumMemberDeclarationSyntax> members)
{
    public EnumDeclarationSyntax EnumDeclarationSyntax { get; } = enumDeclarationSyntax;

    public Compilation Compilation { get; } = compilation;

    public ImmutableArray<EnumMemberDeclarationSyntax> Members { get; } = members;
}
