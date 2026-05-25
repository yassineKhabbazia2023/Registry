// <copyright file="ProspectCreationValidationLogger.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Registry.WebApi.Logging;

/// <summary>
/// Logs automatic prospect-creation validation failures in the registry API.
/// </summary>
public static class ProspectCreationValidationLogger
{
    /// <summary>
    /// Logs the current validation failure with the relevant prospect-creation identifiers.
    /// </summary>
    /// <param name="context">The MVC action context.</param>
    /// <param name="serviceName">The current service name.</param>
    public static void Log(ActionContext context, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("ProspectCreationValidation");
        var validationErrors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        logger.LogWarning(
            "Validation failed for prospect-creation endpoint. ServiceName: {ServiceName}, OperationName: {OperationName}, Route: {Route}, Siret: {Siret}, ProspectId: {ProspectId}, AccountNumber: {AccountNumber}, AccountId: {AccountId}, ContactId: {ContactId}, Email: {Email}, CurrentUserId: {CurrentUserId}, ValidationErrors: {@ValidationErrors}",
            serviceName,
            ResolveOperationName(context),
            context.HttpContext.Request.Path.Value,
            ResolveFirstValue(context, "Siret"),
            ResolveFirstValue(context, "ProspectId"),
            ResolveFirstValue(context, "AccountNumber"),
            ResolveFirstValue(context, "AccountId"),
            ResolveFirstValue(context, "ContactId", "SignatoryContactId", "Contacts[0].ContactId", "CaseManagerContactId"),
            ResolveFirstValue(context, "Email"),
            context.HttpContext.Request.Headers["CurrentUser"].FirstOrDefault(),
            validationErrors);
    }

    /// <summary>
    /// Resolves the current MVC action name.
    /// </summary>
    /// <param name="context">The MVC action context.</param>
    /// <returns>The action name when available; otherwise the display name.</returns>
    private static string? ResolveOperationName(ActionContext context)
    {
        return (context.ActionDescriptor as ControllerActionDescriptor)?.ActionName
            ?? context.ActionDescriptor.DisplayName;
    }

    /// <summary>
    /// Resolves the first available value from route, query string, or model state.
    /// </summary>
    /// <param name="context">The MVC action context.</param>
    /// <param name="candidateNames">The candidate names to inspect.</param>
    /// <returns>The resolved string value when available; otherwise <see langword="null"/>.</returns>
    private static string? ResolveFirstValue(ActionContext context, params string[] candidateNames)
    {
        foreach (var candidateName in candidateNames)
        {
            var routeValue = context.RouteData.Values
                .FirstOrDefault(entry => entry.Key.Equals(candidateName, StringComparison.OrdinalIgnoreCase))
                .Value?
                .ToString();
            if (!string.IsNullOrWhiteSpace(routeValue))
            {
                return routeValue;
            }

            var queryEntry = context.HttpContext.Request.Query
                .FirstOrDefault(entry => entry.Key.Equals(candidateName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(queryEntry.Value.FirstOrDefault()))
            {
                return queryEntry.Value.FirstOrDefault();
            }

            var modelStateValue = TryResolveModelStateValue(context.ModelState, candidateName);
            if (!string.IsNullOrWhiteSpace(modelStateValue))
            {
                return modelStateValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to resolve a value from model state.
    /// </summary>
    /// <param name="modelState">The model state dictionary.</param>
    /// <param name="propertyName">The property name to resolve.</param>
    /// <returns>The resolved value when available; otherwise <see langword="null"/>.</returns>
    private static string? TryResolveModelStateValue(ModelStateDictionary modelState, string propertyName)
    {
        var entry = modelState.FirstOrDefault(item => item.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(entry.Key))
        {
            return null;
        }

        return entry.Value.AttemptedValue
            ?? entry.Value.RawValue?.ToString();
    }

}
