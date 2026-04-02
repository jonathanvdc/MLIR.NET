namespace MLIR.Generators.Emitters;

using System.Text;
using MLIR.ODS.Model;

internal sealed class DialectEmitter
{
    private readonly DialectSymbolResolver resolver;
    private readonly StringBuilder builder = new();

    public DialectEmitter(DialectSymbolResolver resolver)
    {
        this.resolver = resolver;
    }

    public string Generate(DialectModel dialect)
    {
        DialectFileEmitter.EmitHeader(builder, dialect);

        foreach (var operation in dialect.Operations)
        {
            OperationEmitter.Emit(builder, operation, resolver);
            builder.AppendLine();

            if (operation.AssemblyFormat != null)
            {
                var metadata = OperationBodySyntaxEmitter.Emit(builder, operation);
                builder.AppendLine();

                AssemblyFormatEmitter.Emit(builder, operation, metadata, resolver);
                builder.AppendLine();
            }
        }

        foreach (var attribute in dialect.Attributes)
        {
            AttributeEmitter.Emit(builder, attribute);
            builder.AppendLine();
        }

        foreach (var attributeConstraint in dialect.AttributeConstraints)
        {
            AttributeConstraintEmitter.Emit(builder, attributeConstraint);
            builder.AppendLine();
        }

        foreach (var type in dialect.Types)
        {
            TypeEmitter.Emit(builder, type);
            builder.AppendLine();
        }

        DialectRegistrationEmitter.Emit(builder, dialect);

        return builder.ToString();
    }
}
