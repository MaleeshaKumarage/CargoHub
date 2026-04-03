using CargoHub.Application.Bookings;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ImportMappingSignatureTests
{
    [Fact]
    public void NormalizeFileNameKey_UsesBasenameAndLowercase()
    {
        Assert.Equal("data.csv", ImportMappingSignature.NormalizeFileNameKey(@"C:\Temp\Data.CSV"));
        Assert.Equal("x.xlsx", ImportMappingSignature.NormalizeFileNameKey(" folder/X.xlsx "));
    }

    [Fact]
    public void NormalizeFileNameKey_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ImportMappingSignature.NormalizeFileNameKey(null));
        Assert.Equal(string.Empty, ImportMappingSignature.NormalizeFileNameKey("   "));
    }

    [Fact]
    public void BuildHeaderSignature_JoinsTrimmedHeadersInOrder()
    {
        var sig = ImportMappingSignature.BuildHeaderSignature(new[] { " A ", "B" });
        Assert.Equal("A\u001FB", sig);
    }

    [Fact]
    public void BuildHeaderSignature_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ImportMappingSignature.BuildHeaderSignature(null!));
        Assert.Equal(string.Empty, ImportMappingSignature.BuildHeaderSignature(Array.Empty<string>()));
    }

    [Fact]
    public void BuildHeaderSignature_NullCells_TreatedAsEmpty()
    {
        var sig = ImportMappingSignature.BuildHeaderSignature(new[] { "A", null!, " B " });
        Assert.Equal("A\u001F\u001FB", sig);
    }
}
