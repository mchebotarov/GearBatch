using GearBatch.src;
using System.Text;

public class CsvImportParserTests
{
    [Theory]
    [InlineData("sku,productName,category,quantity,unitCostCad", true)]
    [InlineData("Sku,productName,category,quantity,unitCostCad", false)]
    [InlineData("sku,category,productName,quantity,unitCostCad", false)]
    [InlineData("sku,productName,category,quantity", false)]
    public void HasExpectedHeader_RequiresExactColumns(string header, bool expected)
    {
        var columns = header.Split(',');

        var actual = CsvImportParser.HasExpectedHeader(columns);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Parse_ValidSample_ReturnsAllThreeRows()
    {
        var result = ParseSample("""
        TENT-001,Trail Dome 2,Tents,7,125.00
        PACK-030,Ridge Pack 30L,Packs,10,46.50
        LAMP-010,Camp Lantern,Accessories,4,12.00
        """);

        Assert.Null(result.FileError);
        Assert.Empty(result.RowErrors);
        Assert.Equal(new[]
        {
        new ImportRow("TENT-001", "Trail Dome 2", "Tents", 7, 125.00m),
        new ImportRow("PACK-030", "Ridge Pack 30L", "Packs", 10, 46.50m),
        new ImportRow("LAMP-010", "Camp Lantern", "Accessories", 4, 12.00m)
    }, result.AcceptedRows.ToArray());
    }

    [Fact]
    public void Parse_MixedSample_ReturnsTwoRowsAndThreeExactErrors()
    {
        var result = ParseSample("""
        TENT-001,Trail Dome 2,Tents,7,125.00
        PACK-030,Ridge Pack 30L,Packs,-1,46.50
        BOOT-010,Summit Trail Boot,Footwear,3,60.00
        TENT-001,Duplicate Tent,Tents,9,126.00
        bad sku,Invalid Item,Accessories,1,5.00
        """);

        Assert.Null(result.FileError);
        Assert.Equal(new[]
        {
        new ImportRow("TENT-001", "Trail Dome 2", "Tents", 7, 125.00m),
        new ImportRow("BOOT-010", "Summit Trail Boot", "Footwear", 3, 60.00m)
    }, result.AcceptedRows.ToArray());
        Assert.Equal(new[]
        {
        new CsvRowError(3, "INVALID_QUANTITY"),
        new CsvRowError(5, "DUPLICATE_SKU"),
        new CsvRowError(6, "INVALID_SKU")
    }, result.RowErrors.ToArray());
    }

    [Fact]
    public void Parse_QuotedProductName_PreservesCommaAndQuote()
    {
        var result = ParseSample("TENT-001,\"Tent, \"\"2 Person\"\"\",Tents,5,149.99");



        Assert.Null(result.FileError);
        Assert.Empty(result.RowErrors);
        Assert.Equal("Tent, \"2 Person\"", Assert.Single(result.AcceptedRows).ProductName);
    }

    [Fact]
    public void Parse_MalformedSecondRow_DiscardsEarlierAcceptedRow()
    {
        var result = ParseSample("""
        TENT-001,Trail Dome 2,Tents,7,125.00
        PACK-030,"Unclosed quote,Packs,10,46.50
        """);

        Assert.Equal("FILE_INVALID_CSV", result.FileError);
        Assert.Empty(result.AcceptedRows);
        Assert.Empty(result.RowErrors);
    }

    [Fact]
    public void Parse_RowWithInvalidSkuAndQuantity_ReportsOnlyInvalidSku()
    {
        var result = ParseSample("bad sku,Trail Dome 2,Tents,-1,125.00");

        Assert.Null(result.FileError);
        Assert.Empty(result.AcceptedRows);

        var error = Assert.Single(result.RowErrors);
        Assert.Equal(new CsvRowError(2, "INVALID_SKU"), error);
    }

    private static CsvParseResult ParseSample(string rows)
    {
        const string header = "sku,productName,category,quantity,unitCostCad";
        return CsvImportParser.Parse(Encoding.UTF8.GetBytes(header + "\n" + rows));
    }
}