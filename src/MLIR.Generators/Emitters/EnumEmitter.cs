namespace MLIR.Generators.Emitters;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using MLIR.Generators.Emitters.Common;
using MLIR.ODS.Model;

internal static class EnumEmitter
{
    public static string GetEnumConstraintAssemblyFormatTypeName(string constraintRecordName)
    {
        return DialectGeneratorNaming.ToPascalCase(constraintRecordName.Replace('.', '_')) + "ConstraintAttributeValueAssemblyFormat";
    }

    public static string GetIntegerTypeFactoryExpression(int bitwidth) => bitwidth switch
    {
        1 => "global::MLIR.Semantics.TypeFactory.I1",
        8 => "global::MLIR.Semantics.TypeFactory.I8",
        16 => "global::MLIR.Semantics.TypeFactory.I16",
        32 => "global::MLIR.Semantics.TypeFactory.I32",
        64 => "global::MLIR.Semantics.TypeFactory.I64",
        _ => $"global::MLIR.Semantics.TypeFactory.I({bitwidth})",
    };

    public static string GetEnumToIntegerAttrExpression(EnumModel enumModel, string enumValueExpression, string syntaxExpression)
    {
        return "new global::MLIR.Dialects.Builtin.IntegerAttr("
            + GetIntegerTypeFactoryExpression(enumModel.Bitwidth)
            + ", global::MLIR.Numerics.ApInt.FromUInt64("
            + enumModel.Bitwidth
            + ", (ulong)"
            + enumValueExpression
            + "), "
            + syntaxExpression
            + ")";
    }

    public static string GetIntegerToEnumExpression(EnumModel enumModel, string apIntExpression)
    {
        return GetEnumInfoClassName(enumModel)
            + ".FromInteger("
            + apIntExpression
            + ")";
    }

    public static void EmitSharedDefinitions(StringBuilder builder, EnumModel enumModel)
    {
        EmitEnumType(builder, enumModel);
        builder.AppendLine();
        EmitEnumInfo(builder, enumModel);
    }

    public static string GetEnumInfoClassName(EnumModel enumModel)
    {
        return EnumHelpers.GetCSharpEnumTypeName(enumModel) + "Info";
    }

    public static string GetUnderlyingCSharpType(int bitwidth) => bitwidth switch
    {
        8 => "byte",
        16 => "ushort",
        32 => "uint",
        64 => "ulong",
        _ => "ulong",
    };

    public static string GetSeparatorTokenKind(EnumModel enumModel)
    {
        return enumModel.Separator.TrimStart().StartsWith(",", System.StringComparison.Ordinal)
            ? "TokenKind.Comma"
            : "TokenKind.Pipe";
    }

    private static void EmitEnumType(StringBuilder builder, EnumModel enumModel)
    {
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);
        if (enumModel.IsBitEnum)
        {
            builder.AppendLine("[global::System.Flags]");
        }

        builder.AppendLine("public enum " + enumTypeName + " : " + GetUnderlyingCSharpType(enumModel.Bitwidth));
        builder.AppendLine("{");
        foreach (var enumCase in enumModel.Cases)
        {
            var memberName = EnumHelpers.GetCSharpEnumMemberName(enumCase.Symbol);
            builder.AppendLine("    " + memberName + " = " + enumCase.Value + ",");
        }

        builder.AppendLine("}");
    }

    private static void EmitEnumInfo(StringBuilder builder, EnumModel enumModel)
    {
        var enumTypeName = EnumHelpers.GetCSharpEnumTypeName(enumModel);
        var infoClassName = GetEnumInfoClassName(enumModel);

        builder.AppendLine("internal static class " + infoClassName);
        builder.AppendLine("{");

        // NamesByInteger: ApInt-keyed integer-to-name dictionary consumed by the runtime base
        // classes (SimpleEnumAttributeAssemblyFormat<T> / FlagsEnumAttributeAssemblyFormat<T>)
        // so they can print enum values and build the reverse parsing map without depending on
        // the generated enum type at the call site.
        builder.AppendLine("    internal static readonly global::System.Collections.Generic.IReadOnlyDictionary<global::MLIR.Numerics.ApInt, string> NamesByInteger =");
        builder.AppendLine("        new global::System.Collections.Generic.Dictionary<global::MLIR.Numerics.ApInt, string>()");
        builder.AppendLine("        {");
        var seenValuesForNames = new HashSet<long>();
        foreach (var enumCase in enumModel.Cases)
        {
            if (seenValuesForNames.Add(enumCase.Value))
            {
                builder.AppendLine("            { global::MLIR.Numerics.ApInt.FromUInt64(" + enumModel.Bitwidth + ", " + unchecked((ulong)enumCase.Value).ToString() + "UL), " + EmitterHelpers.ToCSharpStringLiteral(enumCase.Str) + " },");
            }
        }

        builder.AppendLine("        };");
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("    internal static " + enumTypeName + " FromInteger(global::MLIR.Numerics.ApInt raw)");
        builder.AppendLine("    {");
        builder.AppendLine("        return (" + enumTypeName + ")raw.ToUInt64();");
        builder.AppendLine("    }");
        builder.AppendLine("}");
    }
}
