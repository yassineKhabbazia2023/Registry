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
                    DelegatorContactId = source.DelegatorContactId,
                    IsDelegation = source.IsDelegation ?? false,
                    IsFavorite = source.IsFavorite ?? false,
                    IsSignatory = source.IsSignatory ?? false
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
