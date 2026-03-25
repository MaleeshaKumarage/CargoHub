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
    public void BuildHeaderSignature_JoinsTrimmedHeadersInOrder()
    {
        var sig = ImportMappingSignature.BuildHeaderSignature(new[] { " A ", "B" });
        Assert.Equal("A\u001FB", sig);
    }
}
