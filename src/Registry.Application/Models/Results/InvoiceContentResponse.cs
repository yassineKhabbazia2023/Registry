// <copyright file="InvoiceContentResponse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models.Results;

/// <summary>
/// An invoice pdf ready to be streamed, together with its display file name.
/// </summary>
/// <remarks>
/// The owner is the download response holding the stream: it must stay alive while the stream is
/// being read, so callers dispose this wrapper only once the response body has been written.
/// </remarks>
public sealed class InvoiceContentResponse : IAsyncDisposable
{
    private readonly IDisposable owner;

    public InvoiceContentResponse(IDisposable owner, Stream content, string fileName)
    {
        this.owner = owner;
        this.Content = content;
        this.FileName = fileName;
    }

    public Stream Content { get; }

    public string FileName { get; }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await this.Content.DisposeAsync();
        this.owner.Dispose();
    }
}
