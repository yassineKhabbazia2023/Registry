// <copyright file="AkuiteoResponseHelper.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Consts;
using Application.Models.Results;

namespace Application.Providers;

/// <summary>
/// Provides shared helpers for parsing Akuiteo HTTP responses.
/// </summary>
internal static class AkuiteoResponseHelper
{
    /// <summary>
    /// Gets the JSON serializer options used for Akuiteo payloads.
    /// </summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new AkuiteoMessageResponseConverter()
        }
    };

    /// <summary>
    /// Deserializes an Akuiteo response payload to the specified type.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="responseBody">The raw JSON response body.</param>
    /// <returns>The deserialized response when valid; otherwise <see langword="null"/>.</returns>
    public static T? Deserialize<T>(string responseBody)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(responseBody, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Indicates whether the Akuiteo metadata status represents a success.
    /// </summary>
    /// <param name="metaStatus">The metadata status.</param>
    /// <returns><see langword="true"/> when the metadata status is successful; otherwise <see langword="false"/>.</returns>
    public static bool IsSucceededStatus(string? metaStatus)
    {
        return string.Equals(metaStatus, AkuiteoMetaStatus.Succeeded, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the error message for a failed Akuiteo metadata status.
    /// </summary>
    /// <param name="meta">The Akuiteo response metadata.</param>
    /// <param name="responseBody">The raw response body.</param>
    /// <returns>The error message.</returns>
    public static string BuildMetaStatusErrorMessage(AkuiteoMetaResponse? meta, string responseBody)
    {
        if (meta == null)
        {
            return string.IsNullOrWhiteSpace(responseBody)
                ? "Akuiteo returned an empty response."
                : "Akuiteo returned an invalid response.";
        }

        var messages = BuildAkuiteoMessagesText(meta.Messages);

        return string.IsNullOrWhiteSpace(messages)
            ? $"Akuiteo returned status '{meta.Status}'."
            : messages;
    }

    /// <summary>
    /// Builds the error message for a downstream non-success response.
    /// </summary>
    /// <param name="responseBody">The raw response body.</param>
    /// <param name="reasonPhrase">The HTTP reason phrase.</param>
    /// <returns>The error message.</returns>
    public static string? BuildDownstreamErrorMessage(string responseBody, string? reasonPhrase)
    {
        var apiResponse = Deserialize<MetaOnlyResponse>(responseBody);
        var messages = BuildAkuiteoMessagesText(apiResponse?.Meta?.Messages);

        if (!string.IsNullOrWhiteSpace(messages))
        {
            return messages;
        }

        return string.IsNullOrWhiteSpace(responseBody) ? reasonPhrase : responseBody;
    }

    private static string BuildAkuiteoMessagesText(IEnumerable<AkuiteoMessageResponse>? messages)
    {
        return messages is null
            ? string.Empty
            : string.Join(
                " ",
                messages
                    .Select(BuildAkuiteoMessageText)
                    .Where(message => !string.IsNullOrWhiteSpace(message)));
    }

    private static string? BuildAkuiteoMessageText(AkuiteoMessageResponse message)
    {
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            return message.Text;
        }

        if (!string.IsNullOrWhiteSpace(message.Message))
        {
            return message.Message;
        }

        return message.Code;
    }

    private sealed class MetaOnlyResponse
    {
        public AkuiteoMetaResponse? Meta { get; set; }
    }

    private sealed class AkuiteoMessageResponseConverter : JsonConverter<AkuiteoMessageResponse>
    {
        public override AkuiteoMessageResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return new AkuiteoMessageResponse
                {
                    Text = reader.GetString()
                };
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return null;
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            return new AkuiteoMessageResponse
            {
                Timestamp = GetStringProperty(root, "timestamp"),
                Code = GetStringProperty(root, "code"),
                Level = GetStringProperty(root, "level"),
                Text = GetStringProperty(root, "text"),
                Message = GetStringProperty(root, "message")
            };
        }

        public override void Write(Utf8JsonWriter writer, AkuiteoMessageResponse value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("timestamp", value.Timestamp);
            writer.WriteString("code", value.Code);
            writer.WriteString("level", value.Level);
            writer.WriteString("text", value.Text);
            writer.WriteString("message", value.Message);
            writer.WriteEndObject();
        }

        private static string? GetStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }
    }
}
