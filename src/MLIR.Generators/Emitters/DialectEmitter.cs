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
            try
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
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate operation '" + (operation.ClassName ?? operation.Name) + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        foreach (var attribute in dialect.Attributes)
        {
            try
            {
                AttributeEmitter.Emit(builder, attribute);
                builder.AppendLine();
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate attribute '" + attribute.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        foreach (var attributeConstraint in dialect.AttributeConstraints)
        {
            try
            {
                AttributeConstraintEmitter.Emit(builder, attributeConstraint);
                builder.AppendLine();
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate attribute constraint '" + attributeConstraint.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        foreach (var type in dialect.Types)
        {
            try
            {
                TypeEmitter.Emit(builder, type);
                builder.AppendLine();
            }
            catch (System.Exception exception)
            {
                throw new System.InvalidOperationException(
                    "Failed to generate type '" + type.RecordName + "' in dialect '" + dialect.Name + "'.",
                    exception);
            }
        }

        DialectRegistrationEmitter.Emit(builder, dialect);

        return builder.ToString();
    }
}
