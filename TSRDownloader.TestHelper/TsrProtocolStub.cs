using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace TSRDownloader.TestHelper;

/// <summary>Configures a StubHttpMessageHandler with the full TSR download protocol.</summary>
public static class TsrProtocolStub
{
    public static StubHttpMessageHandler Create(int itemId, string ticket, string fileName, byte[] zipBytes)
    {
        StubHttpMessageHandler handler = new();

        handler
            .When(r => r.RequestUri!.Query.Contains("a=initDownload"),
                  _ => Json($"{{\"ticket\":\"{ticket}\"}}"))
            .When(r => r.RequestUri!.AbsolutePath.Contains($"/downloads/download/itemId/{itemId}/ticket/"),
                  _ => new HttpResponseMessage(HttpStatusCode.OK))
            .When(r => r.RequestUri!.Query.Contains("a=getdownloadurl"),
                  _ => Json("{\"error\":\"\",\"url\":\"https://cdn.example.test/file.zip\"}"))
            .When(r => r.RequestUri!.Host.Contains("cdn.example.test"),
                  _ => FileResponse(fileName, zipBytes));

        return handler;
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage FileResponse(string fileName, byte[] bytes)
    {
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        };
        response.Content.Headers.ContentDisposition =
            new ContentDispositionHeaderValue("attachment") { FileName = fileName };
        return response;
    }
}
