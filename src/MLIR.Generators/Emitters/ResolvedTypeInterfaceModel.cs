namespace MLIR.Generators.Emitters;

using MLIR.ODS.Model;

internal sealed class ResolvedTypeInterfaceModel
{
    public ResolvedTypeInterfaceModel(string qualifiedName, InterfaceModel interfaceModel)
    {
        QualifiedName = qualifiedName;
        InterfaceModel = interfaceModel;
    }

    public string QualifiedName { get; }

    public InterfaceModel InterfaceModel { get; }
}
