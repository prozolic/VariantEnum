using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace VariantEnum;

internal class VariantEnumContext
{
    public INamedTypeSymbol Symbol { get; }

    public EnumDeclarationSyntax EnumDeclarationSyntax { get; }

    public Compilation Compilation { get; }

    public ImmutableArray<VariantValueTypeMemberDeclarationSyntax> Members { get; }

    public VariantEnumContext(EnumDeclarationSyntax enumDeclarationSyntax, Compilation compilation, ImmutableArray<VariantValueTypeMemberDeclarationSyntax> members)
    {
        EnumDeclarationSyntax = enumDeclarationSyntax;
        Compilation = compilation;
        Members = members;

        var model = Compilation.GetSemanticModel(EnumDeclarationSyntax.SyntaxTree);
        Symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(EnumDeclarationSyntax)!;
    }
}

internal class VariantValueTypeMemberDeclarationSyntax
{
    public EnumMemberDeclarationSyntax MemberSyntax { get;  }

    public bool EnableVariantValueType { get; }

    public VariantValueTypeMemberDeclarationSyntax(EnumMemberDeclarationSyntax memberSyntax, bool enableVariantValueType) 
    {
        MemberSyntax = memberSyntax;
        EnableVariantValueType = enableVariantValueType;
    }
}