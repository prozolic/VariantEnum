using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace VariantEnum;

[Generator(LanguageNames.CSharp)]
public partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource("VariantEnum.g.cs", """
using System;

namespace VariantEnum;

public class VariantValueTypeAttribute : Attribute
{
    public VariantValueTypeAttribute(params Type[] types)
    {
    }
}

""");
        });

        // -Variant
        var variantEnumSource = context.SyntaxProvider
            .CreateSyntaxProvider((node, ct) =>
            {
                if (node.IsKind(SyntaxKind.EnumDeclaration))
                {
                    var enumSyntax = node as EnumDeclarationSyntax;
                    if (enumSyntax == null) return false;

                    // enum XXXX
                    var enumName = enumSyntax.Identifier.Text;
                    return enumName.EndsWith("Variant");
                }

                return false;
            }, static (context, ct) => context)
            .Combine(context.CompilationProvider)
            .Select((gsc, ct) =>
            {
                var (context, compilation) = gsc;
                var enumSyntax = (EnumDeclarationSyntax)context.Node;
                var enumName = enumSyntax.Identifier.Text;

                var variantAttribute = compilation.GetTypeByMetadataName("VariantEnum.VariantValueTypeAttribute");
                var members = enumSyntax.Members.Select(m =>
                {
                    var VariantValueTypeAttribute = m.AttributeLists
                        .SelectMany(attr => attr.Attributes)
                        .FirstOrDefault(attr => attr.Name.ToString() == "VariantValueType")
                        ?.ArgumentList;
                    if (VariantValueTypeAttribute == null)
                    {
                        return m;
                    }

                    var argsTypes = VariantValueTypeAttribute.Arguments.Select(arg =>
                    {
                        var type = arg.Expression as TypeOfExpressionSyntax;
                        if (type == null) return null;

                        var typeSymbol = compilation.GetSemanticModel(type.Type.SyntaxTree).GetTypeInfo(type.Type).Type;
                        return typeSymbol;
                    });

                    var attribute = SyntaxFactory.Attribute(
                        SyntaxFactory.ParseName(variantAttribute!.ToDisplayString()),
                        SyntaxFactory.ParseAttributeArgumentList($"({string.Join(", ", argsTypes)})"));
                    return m.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)));
                }).ToImmutableArray();

                return new VariantEnumContext(enumSyntax, compilation, members);
            });

        context.RegisterSourceOutput(variantEnumSource, Emit);

    }

    private void Emit(SourceProductionContext context, VariantEnumContext source)
    {
        var syntax = source.EnumDeclarationSyntax;
        var varientEnumName = source.EnumDeclarationSyntax.Identifier.Text.Split(["Variant"], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();


        context.AddSource($"{varientEnumName}_generated.g.cs", Emitter.Emit(source, varientEnumName));
    }


    internal class Emitter
    {
        public static string Emit(VariantEnumContext context, string varientEnumName)
        {
            var builder = new StringBuilder(@$"

namespace ConsoleApp1;

public abstract record {varientEnumName} : ISpanFormattable
{{

");


            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}