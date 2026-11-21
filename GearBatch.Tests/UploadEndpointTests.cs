using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

public class UploadEndpointTests : IClassFixture<WebApplicationFactory<GearBatch.Program>>
{
    private const string UploadUrl = "/api/v1/suppliers/SA1/imports";
    private const string SampleCsv =
        "sku,productName,category,quantity,unitCostCad\n" + "TENT-001,Trail Dome 2,Tents,5,125.00";

    private readonly HttpClient client;

    public UploadEndpointTests(WebApplicationFactory<GearBatch.Program> factory)
    {
        client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "test-import-001");
    }

    [Theory]
    [InlineData("test-import.csv", SampleCsv, HttpStatusCode.NotImplemented, null)]
    [InlineData(
        "test-import.txt",
        SampleCsv,
        HttpStatusCode.BadRequest,
        "The uploaded file must end in .csv."
    )]
    [InlineData(
        "empty-import.csv",
        "",
        HttpStatusCode.BadRequest,
        "The uploaded CSV file is empty."
    )]
    public async Task CreateImport_WithMultipartUpload_ReturnsExpectedResponse(
        string fileName,
        string fileContents,
        HttpStatusCode expectedStatus,
        string? expectedError
    )
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Encoding.UTF8.GetBytes(fileContents));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(file, "file", fileName);

        using var response = await client.PostAsync(
            UploadUrl,
            form,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(expectedStatus, response.StatusCode);
        if (expectedError is not null)
        {
            var body = await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken
            );

            Assert.Contains(expectedError, body);
        }
    }

    [Fact]
    public async Task CreateImport_WithJson_ReturnsUnsupportedMediaType()
    {
        using var json = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(
            UploadUrl,
            json,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }
}
