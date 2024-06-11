// <copyright file="AccountService.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using System.Text.Json;

namespace Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            this.accountRepository = accountRepository;
        }

        public async Task StreamAccountsJsonAsync(StreamWriter streamWriter)
        {
            await using var jsonWriter = new Utf8JsonWriter(streamWriter.BaseStream, new JsonWriterOptions { Indented = true });

            jsonWriter.WriteStartArray();

            await foreach (var account in accountRepository.GetAccountsAsync())
            {
                JsonSerializer.Serialize(jsonWriter, new Models.CreAccount
                {
                    AccountNumber = account.AccountNumber,
                    Id = account.Id,
                    LegalName = account.LegalName,
                    Updated = account.Updated,
                });
            }

            jsonWriter.WriteEndArray();
            await jsonWriter.FlushAsync();
        }

        public async Task ProcessAccountAsync(IEnumerable<AccountCsv> accounts)
        {
            var accountsAlx = accounts
                .Select(
                a => new AlxAccount 
                        { 
                            Id = a.Id,
                            LegalName = a.LegalName,
                            AccountNumber = a.AccountNumber,
                            AccountFlagEscActif = a.AccountFlagESCActif
                        }).ToList();
            await this.accountRepository.AddAccountsAsync(accountsAlx);
        }
    }
}
