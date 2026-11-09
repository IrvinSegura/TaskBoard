using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Infrastructure.Email;

internal sealed class WebAppLinks(IOptions<EmailOptions> options) : IWebAppLinks
{
    public string To(string path) => options.Value.WebAppUrl.TrimEnd('/') + path;
}
