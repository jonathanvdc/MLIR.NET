// Polyfill: System.Diagnostics.CodeAnalysis.MaybeNullAttribute was introduced in
// netstandard2.1 / .NET 5. For netstandard2.0 targets we provide a minimal internal copy so
// that nullable-annotated generic method overrides compile without a dependency upgrade.
#if !NETSTANDARD2_1_OR_GREATER && !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false)]
internal sealed class MaybeNullAttribute : Attribute
{
}
#endif
