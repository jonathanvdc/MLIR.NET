namespace TableGen.Tests;

using Xunit;

public sealed class TableGenLibraryTests
{
    [Fact]
    public void ExposesLibraryName()
    {
        Assert.Equal("TableGen", TableGenLibrary.Name);
    }
}
