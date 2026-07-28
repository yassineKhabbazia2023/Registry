/****** Object:  View [reg].[PendingOperations]    Script Date: 23/06/2025 09:52:04 ******/

CREATE view [reg].[PendingOperations]
as
SELECT u.AccountNumber, u.LegalName, u.Id, u.CreationDate, u.ContactEmail, u.FirstName, u.LastName, u.CurrentContactId,
(SELECT TOP 1 r.ContactFlagMainContact FROM ref.Role r WHERE r.ContactEmail=u.ContactEmail AND r.AccountNumber=u.AccountNumber AND r.OperationType='INSERT' ORDER BY r.OperationDate DESC, r.EntityId DESC) as IsSignatory,
(SELECT TOP 1 rc.MobilePhone FROM ref.Contact rc WHERE rc.Email=u.ContactEmail ORDER BY rc.OperationDate DESC, rc.EntityId DESC) as MobilePhone
FROM (
SELECT acc.AccountNumber,acc.LegalName,rop.Id,rop.CreationDate,refro.ContactEmail,refcnt.FirstName,refcnt.LastName,aro.ContactId as CurrentContactId
FROM reg.Operations rop
JOIN ref.Role refro ON refro.EntityId = rop.EntityId
JOIN Account.Roles aro ON aro.AccountNumber = refro.AccountNumber
JOIN Account.Accounts acc ON acc.AccountId=aro.AccountId
join ref.Contact refcnt on refcnt.Email=refro.ContactEmail
where  rop.ApprovalStatus='PENDING' and rop.PublishedAt is null and rop.Type='ROLE' and rop.Operation= 'INSERT'
union
SELECT acc.AccountNumber,acc.LegalName,rop.Id,rop.CreationDate,refro.ContactEmail,cnt.FirstName,cnt.LastName,aro.ContactId as CurrentContactId
FROM reg.Operations rop
JOIN ref.Role refro ON refro.EntityId = rop.EntityId
JOIN Account.Roles aro ON aro.AccountNumber = refro.AccountNumber
JOIN Account.Accounts acc ON acc.AccountId=aro.AccountId
join Contact.Contacts cnt on cnt.Email=refro.ContactEmail
where  rop.ApprovalStatus='PENDING' and rop.PublishedAt is null and rop.Type='ROLE' and rop.Operation= 'INSERT'
) u
GO
