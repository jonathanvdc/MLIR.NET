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
        // Build a map from C++ namespace to dialect so enum constraints can be routed to the
        // dialect that owns their namespace rather than always landing in the prelude.
        var dialectsByCppNamespace = new Dictionary<string, MutableDialectModel>(System.StringComparer.Ordinal);
        foreach (var dialect in dialectsByName.Values)
        {
            if (!string.IsNullOrWhiteSpace(dialect.CppNamespace))
            {
                dialectsByCppNamespace[dialect.CppNamespace!] = dialect;
            }
        }

        // Distribute shared attribute constraints: enum constraints whose cppNamespace matches
        // a known dialect are moved to that dialect so they are generated in the correct C#
        // namespace.  All other constraints remain in the shared prelude pool.
        var preludeConstraints = new List<AttributeConstraintModel>();
        foreach (var constraint in sharedAttributeConstraints)
        {
            var enumCppNamespace = constraint.EnumModel?.CppNamespace;
            if (enumCppNamespace != null
                && dialectsByCppNamespace.TryGetValue(enumCppNamespace, out var targetDialect))
            {
                targetDialect.AttributeConstraints.Add(constraint);
            }
            else
            {
                preludeConstraints.Add(constraint);
            }
        }

        var dialects = dialectsByName.Values
            .Select(static dialect => dialect.ToImmutable())
            .OrderBy(static dialect => dialect.Name, System.StringComparer.Ordinal)
            .ToArray();

        if (dialects.Length == 0)
        {
            return dialects;
        }

        var result = new List<DialectModel>(dialects.Length + 1)
        {
            DialectModel.CreatePrelude(preludeConstraints.ToArray(), sharedTypeConstraints.ToArray()),
        };
        result.AddRange(dialects);
        return result;
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
        public List<AttributeConstraintModel> AttributeConstraints { get; } = new();
        public List<TypeModel> Types { get; } = new();
        public List<TypeConstraintModel> TypeConstraints { get; } = new();

        public DialectModel ToImmutable()
        {
            return new DialectModel(Name, CppNamespace, Summary, Description, HasConstantMaterializer, Operations, Attributes, attributeConstraints: AttributeConstraints, typeConstraints: TypeConstraints, types: Types);
        }
    }
}
