using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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

public sealed class VariantValueTypeAttribute : Attribute
{
    public VariantValueTypeAttribute(params Type[] types)
    {
    }
}

public sealed class IgnoreVariantAttribute : Attribute
{}

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

                    // enum XXXXVariant
                    var enumName = enumSyntax.Identifier.Text;
                    return enumName.EndsWith("Variant") && !(enumName == "Variant");
                }

                return false;
            }, static (context, ct) => context)
            .Combine(context.CompilationProvider)
            .Where(pair => 
            {
                var (context, compilation) = pair;
                var enumSyntax = (EnumDeclarationSyntax)context.Node;
                var model = compilation.GetSemanticModel(enumSyntax.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(enumSyntax);

                return !symbol?.GetAttribute("VariantEnum", "IgnoreVariantAttribute").Any() ?? false;
            })
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
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.MustNotBeNestedType,
                    syntax.Identifier.GetLocation(),
                    syntax.Identifier.Text));
            return;
        }

        var variantEnumName = source.EnumDeclarationSyntax.Identifier.Text.Split(["Variant"], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        context.AddSource($"{variantEnumName}.g.cs", Emitter.Emit(source, variantEnumName));
    }


    internal class Emitter
    {
        public static string Emit(VariantEnumContext context, string variantEnumName)
        {
            var ns = context.Symbol!.ContainingNamespace.IsGlobalNamespace ? string.Empty : $"namespace {context.Symbol!.ContainingNamespace};";
            var code = @$"
// <auto-generated> This .cs file is generated by VariantEnum. </auto-generated>
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

{ns}

public abstract record {variantEnumName} : 
    ISpanFormattable, 
    ISpanParsable<{variantEnumName}>
{{
{EmitMembers(context, variantEnumName)}
    public abstract bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null);

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

{EmitMethod(context, variantEnumName)}
    [DoesNotReturn]
    private static void ThrowRequestedValueNotFound(ReadOnlySpan<char> s)
    {{
        throw new ArgumentException($""Requested value '{{s}}' was not found."");
    }}

    [DoesNotReturn]
    private static T ThrowInvalidType<T>()
    {{
        throw new ArgumentException($""Requested value was invalid type.'"");
    }}
}}";
            return code;
        }

        private static string EmitMembers(VariantEnumContext context, string variantEnumName)
        {
            var builder = new StringBuilder();

            foreach(var member in context.Members)
            {
                var code = @$"    public sealed record {member.MemberSyntax.Identifier.Text}{EmitVariant(context, member)} : {variantEnumName}
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
                var attr = syntax.MemberSyntax.AttributeLists.LastOrDefault()!;
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

        private static string EmitVariant(VariantEnumContext context, VariantValueTypeMemberDeclarationSyntax syntax)
        {
            if (!syntax.EnableVariantValueType) return string.Empty;

            var builder = new StringBuilder();
            var index = 0;
            var attr = syntax.MemberSyntax.AttributeLists.LastOrDefault()!;
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
            var attr = syntax.MemberSyntax.AttributeLists.LastOrDefault();
            if (attr != null)
            {
                variantNameBuilder.AppendLine(@$"
            if (destination.Length < {length + 3})
            {{
                charsWritten += index;
                return false;
            }}");
                foreach (var c in variantName)
                {
                    variantNameBuilder.AppendLine(@$"            destination[index++] = '{c}';");
                }
                variantNameBuilder.AppendLine(@$"            destination[index++] = ' ';
            destination[index++] = '{{';
            destination[index++] = ' ';");

                var arguments = attr.Attributes[0]!.ArgumentList!.Arguments;
                if (arguments.Count > 0)
                {
                    variantNameBuilder.AppendLine("            var handler = new DefaultInterpolatedStringHandler();");
                    var index = 0;
                    foreach (var a in arguments)
                    {
                        variantNameBuilder.AppendLine($"            handler.AppendLiteral(\"args{index} = \");");
                        variantNameBuilder.AppendLine($"            handler.AppendFormatted(args{index++});");

                        if (index < arguments.Count)
                            variantNameBuilder.AppendLine($"            handler.AppendFormatted(\", \" );");
                    }
                    variantNameBuilder.AppendLine("            var print = handler.ToStringAndClear();");
                    variantNameBuilder.AppendLine("            var printSpan = print.AsSpan();");
                    variantNameBuilder.AppendLine(@$"
            if (destination.Length < printSpan.Length + index)
            {{
                charsWritten += index;
                return false;
            }}
            printSpan.CopyTo(destination.Slice(index, printSpan.Length));
            index += printSpan.Length;
            if (destination.Length < 2 + index)
            {{
                charsWritten += index;
                return false;
            }}
            destination[index++] = ' ';
            destination[index++] = '}}';");
                }
            }
            else
            {
                variantNameBuilder.AppendLine(@$"
            if (destination.Length < {length + 4})
            {{
                charsWritten += index;
                return false;
            }}");
                foreach (var c in variantName)
                {
                    variantNameBuilder.AppendLine(@$"            destination[index++] = '{c}';");
                }
                variantNameBuilder.AppendLine(@$"            destination[index++] = ' ';
            destination[index++] = '{{';
            destination[index++] = ' ';
            destination[index++] = '}}';");
            }

            var code = @$"        public override bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {{
            var index = 0;
            charsWritten = 0;
{variantNameBuilder}
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
            
            if (context.Members.Length > 0)
            {
                builder.AppendLine(@$"        return {variantEnumName.ToLower()} switch
        {{");
                foreach (var m in context.Members)
                {
                    var memberName = m.MemberSyntax.Identifier.Text;
                    builder.AppendLine($"            {memberName} => nameof({memberName}),");
                }
                builder.AppendLine($"            _ => null");
                builder.Append(@$"        }};");
            }
            else
            {
                builder.Append($"        return null;");
            }

            var code = @$"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetName({variantEnumName} {variantEnumName.ToLower()})
    {{
{builder}
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
            builder.Append("]");

            var code = @$"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string[] GetNames() => {builder};";
            return code;
        }

        private static string EmitGetNumericValue(VariantEnumContext context, string variantEnumName)
        {
            var builder = new StringBuilder();
            var symbol = context.Symbol;
            
            if (context.Members.Length > 0)
            {
                builder.AppendLine(@$"        return {variantEnumName.ToLower()} switch
        {{");
                foreach (var m in context.Members)
                {
                    var memberName = m.MemberSyntax.Identifier.Text;
                    var value = m.MemberSyntax.EqualsValue;
                    if (value == null)
                    {
                        builder.AppendLine($"            {memberName} => ({symbol.EnumUnderlyingType}){variantEnumName}Variant.{memberName},");
                    }
                    else
                    {
                        var valueText = ((LiteralExpressionSyntax)value.Value).Token.ValueText;
                        builder.AppendLine($"            {memberName} => {valueText},");
                    }
                }
                builder.AppendLine($"            _ => ThrowInvalidType<{symbol.EnumUnderlyingType}>()");
                builder.Append(@$"        }};");
            }
            else
            {
                builder.Append($"        return ThrowInvalidType<{symbol.EnumUnderlyingType}>();");
            }

            var code = @$"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {symbol.EnumUnderlyingType} GetNumericValue({variantEnumName} {variantEnumName.ToLower()})
    {{
{builder}
    }}";
            return code;
        }

        private static string EmitConvertEnum(VariantEnumContext context, string variantEnumName)
        {
            var enumName = $"{variantEnumName}Variant";

            var convertBuilder = new StringBuilder();
            var tryConvertBuilder = new StringBuilder();

            if (context.Members.Length > 0)
            {
                convertBuilder.AppendLine(@$"        return {variantEnumName.ToLower()} switch
        {{");
                tryConvertBuilder.AppendLine(@$"        switch({variantEnumName.ToLower()})
        {{");
                foreach (var memberName in context.Members.Select(m => m.MemberSyntax.Identifier.Text))
                {
                    convertBuilder.AppendLine($"            {memberName} => {enumName}.{memberName},");

                    tryConvertBuilder.AppendLine($"            case {memberName}:");
                    tryConvertBuilder.AppendLine($"                result = {enumName}.{memberName};");
                    tryConvertBuilder.AppendLine($"                return true;");
                }
                convertBuilder.AppendLine($"            _ => ThrowInvalidType<{enumName}>()");
                convertBuilder.Append(@$"        }};");

                tryConvertBuilder.Append(@$"        }}
        result = default;
        return false;");
            }
            else
            {
                convertBuilder.Append($"        return ThrowInvalidType<{enumName}>();");
                tryConvertBuilder.Append(@$"        result = default;
        return false;");
            }

            var code = @$"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {enumName} ConvertEnum({variantEnumName} {variantEnumName.ToLower()})
    {{
{convertBuilder}
    }}

    public static bool TryConvertEnum([NotNullWhen(true)] {variantEnumName}? {variantEnumName.ToLower()}, [MaybeNullWhen(false)] out {enumName} result)
    {{
{tryConvertBuilder}
    }}
";
            return code;
        }

        private static string EmitParse(VariantEnumContext context, string variantEnumName)
        {
            var tryParseBuilder = new StringBuilder();
            var tryParseOrdinalIgnoreCaseBuilder = new StringBuilder();

            if (context.Members.Length > 0)
            {
                tryParseBuilder.AppendLine(@$"            switch (s)
            {{");
                foreach (var memberName in context.Members.Select(m => m.MemberSyntax.Identifier.Text))
                {
                    tryParseBuilder.AppendLine($@"                case ""{memberName}"":");
                    tryParseBuilder.AppendLine($"                    result = {memberName}.Default;");
                    tryParseBuilder.AppendLine($@"                    return true;");

                    tryParseOrdinalIgnoreCaseBuilder.AppendLine(@$"            if (s.Equals(nameof({memberName}), StringComparison.OrdinalIgnoreCase))
            {{
                result = {memberName}.Default;
                return true;
            }}");
                }
                tryParseBuilder.Append(@$"            }}
            result = default;
            return false;");
                tryParseOrdinalIgnoreCaseBuilder.Append(@$"
            result = default;
            return false;");
            }
            else
            {
                tryParseBuilder.Append(@$"            result = default;
            return false;");
                tryParseOrdinalIgnoreCaseBuilder.Append(@$"            result = default;
            return false;");
            }

            var code = @$"    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {variantEnumName} Parse(string s, IFormatProvider? provider = default)
    {{
        return Parse(s.AsSpan(), false, provider);
    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {variantEnumName} Parse(ReadOnlySpan<char> s, IFormatProvider? provider = default)
    {{
        return Parse(s, false, provider);
    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {variantEnumName} Parse(ReadOnlySpan<char> s, bool ignoreCase, IFormatProvider? provider = default)
    {{
        if (TryParse(s, ignoreCase, provider, out var result))
        {{
            return result;
        }}
        else
        {{
            ThrowRequestedValueNotFound(s);
            return default!;
        }}
    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out {variantEnumName} result)
    {{
        return TryParse(s.AsSpan(), false, provider, out result);
    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<char> s, out {variantEnumName} result)
    {{
        return TryParse(s, false, null, out result);
    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out {variantEnumName} result)
    {{
        return TryParse(s, false, null, out result);
    }}

    public static bool TryParse(ReadOnlySpan<char> s, bool ignoreCase, IFormatProvider? provider, [MaybeNullWhen(false)] out {variantEnumName} result)
    {{
        if (ignoreCase)
        {{
{tryParseOrdinalIgnoreCaseBuilder}
        }}
        else
        {{
{tryParseBuilder}
        }}
    }}
";
            return code;
        }

        private static string EmitIsDefined(VariantEnumContext context, string variantEnumName)
        {
            var isDefinedBuilder = new StringBuilder();
            var isDefinedBuilder2 = new StringBuilder();
            if (context.Members.Length > 0)
            {
                isDefinedBuilder.AppendLine(@$"        return s switch
        {{");
                isDefinedBuilder2.AppendLine(@$"        return value switch
        {{");
                foreach (var m in context.Members)
                {
                    var memberName = m.MemberSyntax.Identifier.Text;
                    isDefinedBuilder.AppendLine($"            \"{memberName}\" => true,");
                    isDefinedBuilder2.AppendLine($"            {memberName} => true,");
                }
                isDefinedBuilder.AppendLine($"            _ => false");
                isDefinedBuilder2.AppendLine($"            _ => false");

                isDefinedBuilder.Append(@$"        }};");
                isDefinedBuilder2.Append(@$"        }};");
            }
            else
            {
                isDefinedBuilder.Append("        return false;");
                isDefinedBuilder2.Append("        return false;");
            }

            var code = @$"    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDefined(ReadOnlySpan<char> s)
    {{
{isDefinedBuilder}
    }}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDefined([NotNullWhen(true)] {variantEnumName}? value)
    {{
{isDefinedBuilder2}
    }}
";
            return code;
        }
    }
}