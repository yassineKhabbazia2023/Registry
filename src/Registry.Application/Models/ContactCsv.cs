// <copyright file="ContactCsv.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

namespace Application.Models
{
    /// <summary>
    /// ContactCsv
    /// </summary>
    /// <param name="Id">Id.</param>
    /// <param name="Email">Email.</param>
    /// <param name="FirstName">FirstName.</param>
    /// <param name="LastName">LastName.</param>
    /// <param name="IsCustomer">IsCustomer.</param>
    /// <param name="IsActive">IsActive.</param>
    /// <param name="LandPhone"></param>
    /// <param name="MobilePhone"></param>
    /// <param name="JobDescription"></param>
    /// <param name="OfficeId"></param>
    public record ContactCsv(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        bool IsCustomer,
        bool IsActive,
        string LandPhone,
        string MobilePhone,
        string JobDescription,
        Guid? OfficeId);
}
