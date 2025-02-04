// <copyright file="ReferentialTokenContentHandler.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Providers;

using Application.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

public class ReferentialTokenContentHandler : DelegatingHandler
{
    private readonly ReferentialTokenOptions referentielTokenOptions;

    public ReferentialTokenContentHandler(IOptions<ReferentialTokenOptions> options)
    {
        referentielTokenOptions = options.Value;
    }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {


        var defaultRequestContent = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("grant_type", referentielTokenOptions.GrantType),
                new KeyValuePair<string, string>("client_id", referentielTokenOptions.ClientId),
                new KeyValuePair<string, string>("client_secret", referentielTokenOptions.ClientSecret),
                new KeyValuePair<string, string>("username", referentielTokenOptions.UserName),
                new KeyValuePair<string, string>("password", referentielTokenOptions.Password),
                new KeyValuePair<string, string>("scope", referentielTokenOptions.Scope)
            });

        request.Content = defaultRequestContent;
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        return base.SendAsync(request, cancellationToken);
    }
}

