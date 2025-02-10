using Domain.Entities.Accounts;
using Pulse.Back.Events.IntegrationEvents.EventsData;

namespace Application.Mappers
{
    public static class MapEventDataModelPulse
    {
        public static RoleEntity MapToRoleEntity(this RoleCreatedEventData source)
        {
            if (source == null) {
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
