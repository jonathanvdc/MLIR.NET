namespace MLIR.ODS;

using System.Collections.Generic;
using System.Linq;
using MLIR.ODS.Model;

internal sealed class DialectModelBuilder
{
    private readonly Dictionary<string, MutableDialectModel> dialectsByName = new(System.StringComparer.Ordinal);
    private readonly List<AttributeConstraintModel> sharedAttributeConstraints = new();
    private readonly List<TypeConstraintModel> sharedTypeConstraints = new();

    public MutableDialectModel GetOrCreateDialect(string name)
    {
        if (!dialectsByName.TryGetValue(name, out var dialect))
        {
            dialect = new MutableDialectModel(name);
            dialectsByName.Add(name, dialect);
        }

        return dialect;
    }

    public void AddSharedAttributeConstraint(AttributeConstraintModel constraint)
    {
        sharedAttributeConstraints.Add(constraint);
    }

    public void AddSharedTypeConstraint(TypeConstraintModel constraint)
    {
        sharedTypeConstraints.Add(constraint);
    }

    public IReadOnlyList<DialectModel> Build()
    {
        return dialectsByName.Values
            .Select(dialect => dialect.ToImmutable(sharedAttributeConstraints, sharedTypeConstraints))
            .OrderBy(static dialect => dialect.Name, System.StringComparer.Ordinal)
            .ToArray();
    }

    internal sealed class MutableDialectModel
    {
        public MutableDialectModel(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string? CppNamespace { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public bool HasConstantMaterializer { get; set; }
        public List<OperationModel> Operations { get; } = new();
        public List<AttributeModel> Attributes { get; } = new();
        public List<TypeModel> Types { get; } = new();
        public List<TypeConstraintModel> TypeConstraints { get; } = new();

        public DialectModel ToImmutable(IReadOnlyList<AttributeConstraintModel> sharedAttributeConstraints, IReadOnlyList<TypeConstraintModel> sharedTypeConstraints)
        {
            return new DialectModel(Name, CppNamespace, Summary, Description, HasConstantMaterializer, Operations, Attributes, sharedAttributeConstraints, sharedTypeConstraints, Types);
        }
    }
}
