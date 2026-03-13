// <copyright file="ContactService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Consts;
using Application.Enums;
using Application.Interfaces;
using Application.Mappers;
using Application.Models;
using Application.Models.Contacts;
using Application.Models.Results;
using Application.Requests;
using Microsoft.Extensions.Logging;
using Pulse.Back.Events.IntegrationEvents.EventsData;
using Registry.Application.Consts;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Application.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository contactRepository;
    private readonly ILogger<ContactService> logger;
    private readonly IContactRegistryProvider contactProvider;
    private readonly IOperationService operationService;

    private static readonly Regex FrenchMobileAll = new Regex(
        @"^(\+33|0033|33|0)[67][\s.\-]?(\d{2}[\s.\-]?){3}\d{2}$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250)
    );

    public ContactService(ILogger<ContactService> logger, IContactRepository contactRepository, IContactRegistryProvider provider, IOperationService operationService)
    {
        this.contactRepository = contactRepository;
        this.logger = logger;
        this.contactProvider = provider;
        this.operationService = operationService;
    }

    /// <summary>
    /// if LandPhone is null we take MobilePhone as LandPhone
    /// if MobilePhone is null and LandPhone match whit mebile phone we take it as MobilePhone
    /// if ContactSource is PENNYLANE we should set AccountNumber 
    /// </summary>
    /// <param name="contacts"></param>
    /// <param name="source"></param>
    /// <param name="accountNumber"></param>
    /// <returns></returns>
    public async Task InsertContactsAsync(IEnumerable<RefContactCsv> contacts, string? source = null, string? accountNumber = null)
    {
        var refContactEntities = contacts.MapContactCsvsToContactEntities();

        if (!string.IsNullOrEmpty(source))
        {
            foreach (var contact in refContactEntities)
            {
                if (string.IsNullOrWhiteSpace(contact.LandPhone))
                {
                    contact.LandPhone = contact.MobilePhone;
                }
                else if (string.IsNullOrWhiteSpace(contact.MobilePhone) && FrenchMobileAll.IsMatch(contact.LandPhone.Trim()))
                {
                    contact.MobilePhone = contact.LandPhone;
                }

                contact.ContactSource = source;
                if (source.Equals(DataSources.PENNYLANE.ToString(), StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(accountNumber))
                {
                    contact.AccountNumber = accountNumber;
                }
            }
        }

        await contactRepository.BulkAddContactsAsync(refContactEntities, source);
    }

    public async Task<ContactEventResult<Contact>> OnCreatedContactEventExecution(ContactStateEventData contactStateEventData)
    {
        var contactRegistry = contactStateEventData.ContactStateEventDataToModel();
        var contactModel = contactStateEventData.ContactStateEventDataToContactModel();

        logger.LogInformation($"[Method]: {nameof(OnCreatedContactEventExecution)}  [SubMethod]:{nameof(contactProvider.CreateContactAsync)} Started");

        var isInserted = await this.contactRepository.AddContactAsync(contactModel);
        if (!isInserted)
        {
            logger.LogError($"[Method]: {nameof(OnCreatedContactEventExecution)} ; [Error]: Failed to insert instance contact.Contact in Database.");
        }

        OperationSearchCriteria operationSearchCriteria = new OperationSearchCriteria
        {
            OperationName = OperationAction.Insert,
            OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed },
            OperationApprovalStatus = ApprovalStatus.Approved
        };
        bool updateOperationsSucceeded = await operationService.UpdateContactOperations(operationSearchCriteria, contactModel?.Email);

        if (!updateOperationsSucceeded)
        {
            logger.LogError($"[Method]: {nameof(OnCreatedContactEventExecution)} ; [Error]: Failed to update operation process status list");
        }

        ContactEventResult<Contact> contactEventResult = new ContactEventResult<Contact>
        {
            EventName = "ContactCreatedEventHandler",
            IsSentToAkuiteo = false,
            IsRegisteredInDb = isInserted,
            IsOpeationProcessUpdated = updateOperationsSucceeded,
            Content = contactModel
        };

        return contactEventResult;
    }

    public async Task<ContactEventResult<Contact>> OnUpdatedContactEventExecution(ContactStateEventData contactStateEventData)
    {
        var contactRegistry = contactStateEventData.ContactStateEventDataToModel();
        var contactModel = contactStateEventData.ContactStateEventDataToContactModel();

        logger.LogInformation($"[Method]: {nameof(OnUpdatedContactEventExecution)}  [SubMethod]:{nameof(contactProvider.UpdateContactAsync)} Started");
        #region legacy code
        //var httpResponseMessage = await contactProvider.UpdateContactAsync(contactRegistry);
        //var httpSuccess = httpResponseMessage.IsSuccessStatusCode;
        //if (!httpSuccess)
        //{
        //    logger.LogError($"[Method]: {nameof(OnUpdatedContactEventExecution)} ; [Error]:Could not send request UpdateContactAsync to Akuiteo");
        //}
        #endregion
        var isUpdated = await this.contactRepository.UpdateContactAsync(contactModel);
        if (!isUpdated)
        {
            logger.LogError($"[Method]: {nameof(OnUpdatedContactEventExecution)} ; [Error]: Failed to update instance contact.Contact in Database.");
        }

        OperationSearchCriteria operationSearchCriteria = new OperationSearchCriteria
        {
            OperationName = "UPDATE",
            OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed },
            OperationApprovalStatus = ApprovalStatus.Approved
        };
        bool updateOperationsSucceeded = await operationService.UpdateContactOperations(operationSearchCriteria, contactModel?.Email);

        if (!updateOperationsSucceeded)
        {
            logger.LogError($"[Method]: {nameof(OnUpdatedContactEventExecution)} ; [Error]: Failed to update operation process status list");
        }

        ContactEventResult<Contact> contactEventResult = new ContactEventResult<Contact>
        {
            EventName = "ContactUpdatedEventHandler",
            IsSentToAkuiteo = false,
            IsRegisteredInDb = isUpdated,
            IsOpeationProcessUpdated = updateOperationsSucceeded,
            Content = contactModel
        };

        return contactEventResult;
    }

    public async Task<ContactEventResult<Contact>> OnRemovedContactEventExecution(ContactRemovedEventData contactRemovedData)
    {
        bool isSentToAkuiteo = false;
        bool isRegisteredInDb = false;

        bool updateOperationProcessStatusSucceed = false;
        OperationSearchCriteria operationSearchCriteria = new OperationSearchCriteria
        {
            OperationName = "DELETE",
            OperationProcessStatus = new string[] { ProcessStatus.Sent, ProcessStatus.Failed },
            OperationApprovalStatus = ApprovalStatus.Approved
        };
        int contactId = contactRemovedData.ContactId;
        Contact? contact = await this.contactRepository.GetContactByEmailOrIdAsync(contactId: contactId);

        if (contact == null)
        {
            logger.LogError($"[Method]: {nameof(OnRemovedContactEventExecution)} ; [Error]: Failed to Deleted instance contact.Contact with contactId {contactId} because contact does not exist!");
        }
        else
        {
            isRegisteredInDb = await this.contactRepository.DeleteContactByIdAsync(contactRemovedData.ContactId);
            if (!isRegisteredInDb)
            {
                logger.LogError($"[Method]: {nameof(OnRemovedContactEventExecution)} ; [Error]: Something went wrong while Deleting Contact {contact.ContactId}");
            }

            updateOperationProcessStatusSucceed = await operationService.UpdateContactOperations(operationSearchCriteria, contact?.Email);
            if (!updateOperationProcessStatusSucceed)
            {
                logger.LogError($"[Method]: {nameof(OnRemovedContactEventExecution)} ; [Error]: Failed to update operation process status list");
            }
        }

        return new ContactEventResult<Contact>
        {
            EventName = "ContactRemovedEventHandler",
            IsOpeationProcessUpdated = updateOperationProcessStatusSucceed,
            IsRegisteredInDb = isRegisteredInDb,
            IsSentToAkuiteo = isSentToAkuiteo,
            Content = contact
        };
    }

}
