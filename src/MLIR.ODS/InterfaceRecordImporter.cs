namespace MLIR.ODS;

/// <summary>
/// Imports interface definitions from evaluated TableGen records (records that have
/// <c>Interface</c> as a base class).
/// </summary>
internal static class InterfaceRecordImporter
{
    public static void Import(OdsRecordIndex index, DialectModelBuilder builder)
    {
        foreach (var record in index.GetRecordsWithBaseClass("Interface"))
        {
            var interfaceModel = index.TryBuildInterfaceModel(record);
            if (interfaceModel != null)
            {
                builder.AddSharedInterface(interfaceModel);
            }
        }
    }
}
