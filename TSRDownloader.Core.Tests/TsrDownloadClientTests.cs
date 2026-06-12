using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using TSRDownloader.Core.Exceptions;
using TSRDownloader.Core.Models;
using TSRDownloader.Core.Services;
using TSRDownloader.TestHelper;
using Xunit;

namespace TSRDownloader.Core.Tests;

public class TsrDownloadClientTests
{
    private static TsrDownloadClient ClientWith(StubHttpMessageHandler handler)
    {
        HttpClient http = new(handler);
        return new TsrDownloadClient(http, () => TimeSpan.Zero);
    }

    [Fact]
    public async Task ResolveDownloadAsync_Should_ReturnInfoWithFileName_When_ProtocolSucceeds()
    {
        // Arrange
        byte[] zip = Encoding.UTF8.GetBytes("PK-fake");
        StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", "mod.zip", zip);

        // Act
        TsrDownloadInfo info = await ClientWith(handler).ResolveDownloadAsync(1, CancellationToken.None);

        // Assert
        info.FileName.ShouldBe("mod.zip");
        info.DownloadUrl.ShouldContain("cdn.example.test");
    }

    [Fact]
    public async Task ResolveDownloadAsync_Should_Throw_When_ResolveReturnsError()
    {
        // Arrange
        StubHttpMessageHandler handler = new StubHttpMessageHandler()
            .When(r => r.RequestUri!.Query.Contains("a=initDownload"),
                  _ => new HttpResponseMessage(HttpStatusCode.OK)
                  { Content = new StringContent("{\"ticket\":\"tkt\"}", Encoding.UTF8, "application/json") })
            .When(r => r.RequestUri!.AbsolutePath.Contains("/downloads/download/itemId/1/ticket/"),
                  _ => new HttpResponseMessage(HttpStatusCode.OK))
            .When(r => r.RequestUri!.Query.Contains("a=getdownloadurl"),
                  _ => new HttpResponseMessage(HttpStatusCode.OK)
                  { Content = new StringContent("{\"error\":\"bad ticket\",\"url\":\"\"}", Encoding.UTF8, "application/json") });

        // Act / Assert
        await Should.ThrowAsync<InvalidDownloadTicketException>(() =>
            ClientWith(handler).ResolveDownloadAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadFileAsync_Should_WriteFileAndReturnPath_When_Downloaded()
    {
        // Arrange
        using TempDir temp = new();
        byte[] zip = Encoding.UTF8.GetBytes("file-content");
        StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", "mod.zip", zip);
        TsrDownloadClient client = ClientWith(handler);
        TsrDownloadInfo info = await client.ResolveDownloadAsync(1, CancellationToken.None);

        // Act
        string path = await client.DownloadFileAsync(info, temp.Path, null, CancellationToken.None);

        // Assert
        path.ShouldBe(Path.Combine(temp.Path, "mod.zip"));
        File.ReadAllBytes(path).ShouldBe(zip);
    }

    [Fact]
    public async Task ResolveDownloadAsync_Should_StripForbiddenCharacters_When_FileNameHasThem()
    {
        // Arrange
        byte[] zip = Encoding.UTF8.GetBytes("x");
        StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", "my:mod.zip", zip);

        // Act
        TsrDownloadInfo info = await ClientWith(handler).ResolveDownloadAsync(1, CancellationToken.None);

        // Assert
        info.FileName.ShouldNotContain(":");
        info.FileName.ShouldEndWith("mod.zip");
    }

    [Fact]
    public async Task ResolveDownloadAsync_Should_ReadCurrentDelay_When_EachDownloadStarts()
    {
        // Arrange
        StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", "mod.zip", Encoding.UTF8.GetBytes("PK"));
        int delayReads = 0;
        TsrDownloadClient client = new(new HttpClient(handler), () =>
        {
            delayReads++;
            return TimeSpan.Zero;
        });

        // Act
        await client.ResolveDownloadAsync(1, CancellationToken.None);
        await client.ResolveDownloadAsync(1, CancellationToken.None);

        // Assert
        delayReads.ShouldBe(2);
    }

    [Fact]
    public async Task ResolveDownloadAsync_Should_WaitForConfiguredDelay_When_DelayIsSet()
    {
        // Arrange
        StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", "mod.zip", Encoding.UTF8.GetBytes("PK"));
        TsrDownloadClient client = new(new HttpClient(handler), () => TimeSpan.FromMilliseconds(300));
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await client.ResolveDownloadAsync(1, CancellationToken.None);

        // Assert
        stopwatch.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250));
    }

    private static TsrDownloadInfo CdnInfo(string fileName = "mod.zip") =>
        new(1, "https://cdn.example.test/file.zip", fileName);

    [Fact]
    public async Task DownloadFileAsync_Should_AppendToPartFile_When_ServerHonoursRange()
    {
        // Arrange
        using TempDir temp = new();
        File.WriteAllText(Path.Combine(temp.Path, "mod.zip.part"), "file-");
        StubHttpMessageHandler handler = new StubHttpMessageHandler()
            .When(r => r.Headers.Range is not null,
                  _ => new HttpResponseMessage(HttpStatusCode.PartialContent)
                  { Content = new StringContent("content") });

        // Act
        string path = await ClientWith(handler).DownloadFileAsync(CdnInfo(), temp.Path, null, CancellationToken.None);

        // Assert
        File.ReadAllText(path).ShouldBe("file-content");
    }

    [Fact]
    public async Task DownloadFileAsync_Should_RestartFromScratch_When_ServerIgnoresRange()
    {
        // Arrange
        using TempDir temp = new();
        File.WriteAllText(Path.Combine(temp.Path, "mod.zip.part"), "stale-");
        StubHttpMessageHandler handler = new StubHttpMessageHandler()
            .When(_ => true, _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("full") });

        // Act
        string path = await ClientWith(handler).DownloadFileAsync(CdnInfo(), temp.Path, null, CancellationToken.None);

        // Assert
        File.ReadAllText(path).ShouldBe("full");
    }

    [Fact]
    public async Task DownloadFileAsync_Should_RestartFromScratch_When_RangeIsNotSatisfiable()
    {
        // Arrange
        using TempDir temp = new();
        File.WriteAllText(Path.Combine(temp.Path, "mod.zip.part"), "stale-and-too-long");
        StubHttpMessageHandler handler = new StubHttpMessageHandler()
            .When(r => r.Headers.Range is not null,
                  _ => new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable))
            .When(_ => true, _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("full") });

        // Act
        string path = await ClientWith(handler).DownloadFileAsync(CdnInfo(), temp.Path, null, CancellationToken.None);

        // Assert
        File.ReadAllText(path).ShouldBe("full");
    }

    [Theory]
    [InlineData("..")]
    [InlineData("...")]
    [InlineData("\u0001\u0002")]
    public async Task ResolveDownloadAsync_Should_UseFallbackName_When_FileNameIsUnusable(string fileName)
    {
        // Arrange
        StubHttpMessageHandler handler = TsrProtocolStub.Create(1, "tkt", fileName, Encoding.UTF8.GetBytes("x"));

        // Act
        TsrDownloadInfo info = await ClientWith(handler).ResolveDownloadAsync(1, CancellationToken.None);

        // Assert
        info.FileName.ShouldBe("download.zip");
    }

    [Fact]
    public async Task ResolveDownloadAsync_Should_ThrowTicketException_When_ResponseLacksFields()
    {
        // Arrange
        StubHttpMessageHandler handler = new StubHttpMessageHandler()
            .When(r => r.RequestUri!.Query.Contains("a=initDownload"),
                  _ => new HttpResponseMessage(HttpStatusCode.OK)
                  { Content = new StringContent("{\"ticket\":\"tkt\"}", Encoding.UTF8, "application/json") })
            .When(r => r.RequestUri!.AbsolutePath.Contains("/downloads/download/itemId/1/ticket/"),
                  _ => new HttpResponseMessage(HttpStatusCode.OK))
            .When(r => r.RequestUri!.Query.Contains("a=getdownloadurl"),
                  _ => new HttpResponseMessage(HttpStatusCode.OK)
                  { Content = new StringContent("{}", Encoding.UTF8, "application/json") });

        // Act / Assert
        await Should.ThrowAsync<InvalidDownloadTicketException>(() =>
            ClientWith(handler).ResolveDownloadAsync(1, CancellationToken.None));
    }
}
