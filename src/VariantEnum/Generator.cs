﻿using Microsoft.CodeAnalysis;
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
                        return new VariantValueTypeMemberDeclarationSyntax(m, false);
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

                    return new VariantValueTypeMemberDeclarationSyntax(
                        m.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))),
                        true);
                }).ToImmutableArray();

                return new VariantEnumContext(enumSyntax, compilation, members);
            });

        context.RegisterSourceOutput(variantEnumSource, Emit);

    }

    private void Emit(SourceProductionContext context, VariantEnumContext source)
    {
        var syntax = source.EnumDeclarationSyntax;

        if (source.EnumDeclarationSyntax.IsNested())
        {
            // error
            return;
        }

        var variantEnumName = source.EnumDeclarationSyntax.Identifier.Text.Split(["Variant"], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        context.AddSource($"{variantEnumName}_generated.g.cs", Emitter.Emit(source, variantEnumName));
    }


    internal class Emitter
    {
        public static string Emit(VariantEnumContext context, string variantEnumName)
        {
            var ns = context.Symbol!.ContainingNamespace.IsGlobalNamespace ? string.Empty : $"namespace {context.Symbol!.ContainingNamespace};";
            var code = @$"
// <auto-generated> This .cs file is generated by ValueEnum. </auto-generated>
#nullable enable
#pragma warning disable CS0219 // The variable 'variable' is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8619 // Possible null reference assignment fix

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

{ns}

public abstract record {variantEnumName} : ISpanFormattable
{{
{EmitMembers(context, variantEnumName)}
    public abstract bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null);

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

{EmitMethod(context, variantEnumName)}
}}";
            return code;
        }

        private static string EmitMembers(VariantEnumContext context, string variantEnumName)
        {
            var builder = new StringBuilder();

            foreach(var member in context.Members)
            {
                var code = @$"    public sealed record {member.MemberSyntax.Identifier.Text}{Emitvariant(context, member)} : {variantEnumName}
    {{
{EmitDefault(member)}

{EmitSpanFormattable(member, variantEnumName)}
    }}
";
                builder.AppendLine(code);
            }
            return builder.ToString();
        }

        private static string EmitDefault(VariantValueTypeMemberDeclarationSyntax syntax)
        {
            if (syntax.MemberSyntax.AttributeLists.Count == 0)
            {
                var code = @$"        public static {syntax.MemberSyntax.Identifier.Text} Default => new {syntax.MemberSyntax.Identifier.Text}();";
                return code;
            }
            else
            {

                var index = 0;
                var attr = syntax.MemberSyntax.AttributeLists.LastOrDefault();
                var arguments = attr.Attributes[0]!.ArgumentList!.Arguments;
                var builder = new StringBuilder();
                foreach (var a in arguments)
                {
                    builder.Append($"args{index++}: default");

                    if (index < arguments.Count)
                        builder.Append(", ");
                }

                var code = @$"        public static {syntax.MemberSyntax.Identifier.Text} Default => new {syntax.MemberSyntax.Identifier.Text}({builder});";
                return code;
            }
        }

        private static string Emitvariant(VariantEnumContext context, VariantValueTypeMemberDeclarationSyntax syntax)
        {
            if (!syntax.EnableVariantValueType) return string.Empty;

            var builder = new StringBuilder();
            var index = 0;
            var attr = syntax.MemberSyntax.AttributeLists.LastOrDefault();
            var arguments = attr.Attributes[0]!.ArgumentList!.Arguments;
            builder.Append("(");
            foreach (var a in arguments)
            {
                builder.Append($"{a} args{index++}");

                if (index < arguments.Count)
                    builder.Append(", ");
            }
            builder.Append(")");

            return builder.ToString();
        }

        private static string EmitSpanFormattable(VariantValueTypeMemberDeclarationSyntax syntax, string variantEnumName)
        {
            var variantName = syntax.MemberSyntax.Identifier.Text;
            var length = syntax.MemberSyntax.Identifier.Text.Length;

            var variantNameBuilder = new StringBuilder();
            variantNameBuilder.AppendLine(@$"
            if (destination.Length < {length} + index)
            {{
                charsWritten += index;
                return false;
            }}");
            foreach(var c in variantName)
            {
                variantNameBuilder.AppendLine(@$"            destination[index++] = '{c}';");
            }

            var parameterBuilder = new StringBuilder();
            var attr = syntax.MemberSyntax.AttributeLists.LastOrDefault();
            if (attr != null)
            {
                var arguments = attr.Attributes[0]!.ArgumentList!.Arguments;
                if (arguments.Count > 0)
                {
                    parameterBuilder.AppendLine("            var handler = new DefaultInterpolatedStringHandler();");
                    var index = 0;
                    foreach (var a in arguments)
                    {
                        parameterBuilder.AppendLine($"            handler.AppendLiteral(\"args{index} = \");");
                        parameterBuilder.AppendLine($"            handler.AppendFormatted(args{index++});");

                        if (index < arguments.Count)
                            parameterBuilder.AppendLine($"            handler.AppendFormatted(\", \" );");
                    }
                    parameterBuilder.AppendLine("            var print = handler.ToStringAndClear();");
                    parameterBuilder.AppendLine("            var printSpan = print.AsSpan();");
                    parameterBuilder.AppendLine(@$"
            if (destination.Length < printSpan.Length + index)
            {{
                charsWritten += index;
                return false;
            }}
            printSpan.CopyTo(destination.Slice(index, printSpan.Length));
            index += printSpan.Length;");
                }
            }

            var code = @$"        public override bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {{
            var index = 0;
            charsWritten = 0;

            if (destination.Length < {length})
            {{
                charsWritten += index;
                return false;
            }}
{variantNameBuilder}
            if (destination.Length < 3 + index)
            {{
                charsWritten += index;
                return false;
            }}
            destination[index++] = ' ';
            destination[index++] = '{{';
            destination[index++] = ' ';
{parameterBuilder}
            if (destination.Length < 2 + index)
            {{
                charsWritten += index;
                return false;
            }}
            destination[index++] = ' ';
            destination[index++] = '}}';

            charsWritten = index;
            return true;
        }}
";
            return code;
        }

        private static string EmitMethod(VariantEnumContext context, string variantEnumName)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"    public static int Count => {context.Members.Length};");
            builder.AppendLine(EmitGetName(context, variantEnumName));
            builder.AppendLine(EmitGetNames(context, variantEnumName));
            builder.AppendLine(EmitGetNumericValue(context, variantEnumName));
            builder.AppendLine(EmitConvertEnum(context, variantEnumName));
            builder.AppendLine(EmitParse(context, variantEnumName));
            builder.Append(EmitIsDefined(context, variantEnumName));

            return builder.ToString();
        }

        private static string EmitGetName(VariantEnumContext context, string variantEnumName)
        {
            var builder = new StringBuilder();
            var symbol = context.Symbol;
            foreach (var m in context.Members)
            {
                var memberName = m.MemberSyntax.Identifier.Text;
                builder.AppendLine($"            {memberName} => nameof({memberName}),");
            }
            builder.AppendLine($"            _ => throw new InvalidOperationException(nameof({variantEnumName.ToLower()}))");

            var code = @$"
    public static string GetName({variantEnumName} {variantEnumName.ToLower()})
    {{
        return {variantEnumName.ToLower()} switch
        {{
{builder}
        }};
    }}";
            return code;
        }

        private static string EmitGetNames(VariantEnumContext context, string variantEnumName)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            for (var index = 0; index < context.Members.Length; index++)
            {
                builder.Append($@"nameof({context.Members[index].MemberSyntax.Identifier.Text})");
                if (index < context.Members.Length - 1)
                    builder.Append(", ");
            }
            builder.Append("];");

            var code = @$"
    public static string[] GetNames()
    {{
        return {builder}
    }}";
            return code;
        }

        private static string EmitGetNumericValue(VariantEnumContext context, string variantEnumName)
        {
            var builder = new StringBuilder();
            var symbol = context.Symbol;
            foreach (var m in context.Members)
            {
                var memberName = m.MemberSyntax.Identifier.Text;
                var value = m.MemberSyntax.EqualsValue;
                if (value == null)
                {
                    builder.AppendLine($"            {memberName} => ({symbol.EnumUnderlyingType}){variantEnumName}Variant.{m.MemberSyntax.Identifier.Text},");
                }
                else
                {
                    var valueText = ((LiteralExpressionSyntax)value.Value).Token.ValueText;
                    builder.AppendLine($"            {memberName} => {valueText},");
                }
            }
            builder.AppendLine($"            _ => throw new InvalidOperationException(nameof({variantEnumName.ToLower()}))");

            var code = @$"
    public static {symbol.EnumUnderlyingType} GetNumericValue({variantEnumName} {variantEnumName.ToLower()})
    {{
        return {variantEnumName.ToLower()} switch
        {{
{builder}
        }};
    }}";
            return code;
        }

        private static string EmitConvertEnum(VariantEnumContext context, string variantEnumName)
        {
            var enumName = $"{variantEnumName}Variant";

            var convertBuilder = new StringBuilder();
            foreach(var m in context.Members)
            {
                convertBuilder.AppendLine($"            {m.MemberSyntax.Identifier.Text} => {enumName}.{m.MemberSyntax.Identifier.Text},");
            }
            convertBuilder.AppendLine($"            _ => throw new InvalidOperationException(nameof({variantEnumName.ToLower()}))");

            var tryConvertBuilder = new StringBuilder();
            foreach (var m in context.Members)
            {
                tryConvertBuilder.AppendLine($"            case {m.MemberSyntax.Identifier.Text}:");
                tryConvertBuilder.AppendLine($"                {variantEnumName.ToLower()}Variant = {enumName}.{m.MemberSyntax.Identifier.Text};");
                tryConvertBuilder.AppendLine($"                return true;");
            }

            var code = @$"
    public static {enumName} ConvertEnum({variantEnumName} {variantEnumName.ToLower()})
    {{
        return {variantEnumName.ToLower()} switch
        {{
{convertBuilder}
        }};
    }}

    public static bool TryConvertEnum({variantEnumName} {variantEnumName.ToLower()}, out {enumName} {variantEnumName.ToLower()}Variant)
    {{
        switch({variantEnumName.ToLower()})
        {{
{tryConvertBuilder}
        }}
        {variantEnumName.ToLower()}Variant = default;
        return false;
    }}
";
            return code;
        }

        private static string EmitParse(VariantEnumContext context, string variantEnumName)
        {
            var parseBuilder = new StringBuilder();
            foreach (var m in context.Members)
            {
                var memberName = m.MemberSyntax.Identifier.Text;
                parseBuilder.AppendLine($"            \"{memberName}\" => {memberName}.Default,");
            }
            parseBuilder.AppendLine($"            _ => throw new InvalidOperationException(nameof(value))");

            var tryConvertBuilder = new StringBuilder();
            foreach (var m in context.Members)
            {
                var memberName = m.MemberSyntax.Identifier.Text;
                tryConvertBuilder.AppendLine($@"            case ""{m.MemberSyntax.Identifier.Text}"":");
                tryConvertBuilder.AppendLine($"                {variantEnumName.ToLower()} = {memberName}.Default;");
                tryConvertBuilder.AppendLine($"                return true;");
            }

            var code = @$"    public static {variantEnumName} Parse(string value)
    {{
        return value switch
        {{
{parseBuilder}
        }};
    }}

    public static bool TryParse(string value, out {variantEnumName} {variantEnumName.ToLower()})
    {{
        switch (value)
        {{
{tryConvertBuilder}
        }}
        {variantEnumName.ToLower()} = default;
        return false;
    }}
";
            return code;
        }

        private static string EmitIsDefined(VariantEnumContext context, string variantEnumName)
        {
            var isDefinedBuilder = new StringBuilder();
            var isDefinedBuilder2 = new StringBuilder();
            foreach (var m in context.Members)
            {
                var memberName = m.MemberSyntax.Identifier.Text;
                isDefinedBuilder.AppendLine($"            \"{memberName}\" => true,");
                isDefinedBuilder2.AppendLine($"            {memberName} => true,");
            }
            isDefinedBuilder.AppendLine($"            _ => false");
            isDefinedBuilder2.AppendLine($"            _ => false");

            var code = @$"    public static bool IsDefined(string value)
    {{
        return value switch
        {{
{isDefinedBuilder}
        }};
    }}

    public static bool IsDefined({variantEnumName} value)
    {{
        return value switch
        {{
{isDefinedBuilder2}
        }};
    }}
";
            return code;
        }
    }
}