// <copyright file="MapEventDataModelPulse.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Pulse.Back.Events.IntegrationEvents.EventsData;
using Pulse.Registry.Domain.Entities.Accounts;

namespace Application.Mappers
{
    public static class MapEventDataModelPulse
    {
        public static RoleEntity MapToRoleEntity(this RoleCreatedEventData source)
        {
            if (source == null)
            {
                return null!;
            }
            else
            {
                return new RoleEntity
                {
                    AccountGlobalUniqueId = source.AccountGlobalUniqueId,
                    AccountId = source.AccountId,
                    AccountNumber = source.AccountNumber,
                    ContactEmail = source.ContactEmail,
                    ContactGlobalUniqueId = source.ContactGlobalUniqueId,
                    ContactId = source.ContactId,
                    ContactFlagPortailFactures = source.ContactFlagPortailFactures,
                    ContactFlagMainContact = source.IsSignatory,
                    RoleDuplicatesCounter = 1,
                };
            }
        }

        public static RoleEntity MapToRoleEntity(this RoleDeletedEventData source)
        {
            if (source == null)
            {
                return null!;
            }
            else
            {
                return new RoleEntity
                {
                    AccountGlobalUniqueId = source.AccountGlobalUniqueId,
                    AccountId = source.AccountId,
                    AccountNumber = source.AccountNumber,
                    ContactEmail = source.ContactEmail,
                    ContactId = source.ContactId
                };
            }
        }
    }
}
