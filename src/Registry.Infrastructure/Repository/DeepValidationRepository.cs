using Application.Interfaces;
using Application.Models;
using Application.Models.Audits;
using Domain.Entities.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pulse.Registry.Domain.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repository
{
    public class DeepValidationRepository : IDeepValidationRepository
    {
        private readonly RefContext _refContext;
        private readonly ILogger<DeepValidationRepository> _logger;


        public DeepValidationRepository(RefContext refContext, ILogger<DeepValidationRepository> logger)
        {
            _refContext = refContext;
            _logger = logger;
        }


        public async Task<bool> AddDeepValidationAsync(DeepValidationEntity deepValidation)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(nameof(deepValidation));
            return await TryReposAction<DeepValidationEntity>(async (deepValidation) =>
            {
                await _refContext.AddAsync(deepValidation);
                await _refContext.SaveChangesAsync();
                return true;
            }, deepValidation);
        }

        private async Task<bool> TryReposAction<T>(Func<T, Task<bool>> functionExecution, T t)
        {
            try
            {
                return await functionExecution(t);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError($"[Method]: {nameof(functionExecution)}; [Error]: {dbEx.Message}");
                return false;
            }
            catch (InvalidOperationException ioEx)
            {
                _logger.LogError($"[Method]: {nameof(functionExecution)}; [Error]: {ioEx.Message}");
                return false;
            }
        }

    }
}
