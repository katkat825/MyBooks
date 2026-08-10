using System.Net;
using System.Text;

namespace MyBooks.AuthService.Tests.Infrastructure;

/// <summary>
/// InvitationService and TenantClient are concrete classes with no virtual members, so
/// they cannot be substituted. The only seam is the transport, so tests drive them
/// through a scripted handler and assert on the requests that came out.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public List<HttpRequestMessage> Requests { get; } = new();
    public List<string> Bodies { get; } = new();

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => _responder = responder;

    public static StubHttpMessageHandler AlwaysReturns(HttpStatusCode status, string body = "")
        => new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });

    /// <summary>
    /// Answers the system-token handshake first, then the caller-supplied response for
    /// everything else. Every service in this solution fetches a system token before it
    /// talks to a sibling service.
    /// </summary>
    public static StubHttpMessageHandler WithSystemToken(
        HttpStatusCode downstreamStatus,
        string downstreamBody = "")
        => new(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/system/token", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"token\":\"stub-system-token\"}", Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(downstreamStatus)
            {
                Content = new StringContent(downstreamBody, Encoding.UTF8, "application/json")
            };
        });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        Bodies.Add(request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken));

        return _responder(request);
    }

    public bool SentTo(string absolutePathSuffix)
        => Requests.Any(r => r.RequestUri!.AbsolutePath
            .EndsWith(absolutePathSuffix, StringComparison.Ordinal));
}
