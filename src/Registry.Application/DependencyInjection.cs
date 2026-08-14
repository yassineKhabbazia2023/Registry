// <copyright file="DependencyInjection.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.DeepValidation;
using Application.Factories;
using Application.Helpers;
using Application.Interfaces;
using Application.services;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Application
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IContactService, ContactService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IOperationService, OperationService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IAccountDeepValidationService, AccountDeepValidationSerivce>();
            services.AddSingleton(typeof(IValidationHelper<>),typeof(ValidationHelper<>));
            services.AddScoped<IRoleDeepValidatorFactory, RoleDeepValidatorFactory>();
            services.AddScoped<IContactsDeepValidationsService, ContactsDeepValidationsService>();
            services.AddScoped<IOfferService,OfferService>();
            services.AddScoped<IHubSpotService, HubSpotService>();
            services.AddScoped<IProspectEligibilityService, ProspectEligibilityService>();
            services.AddScoped<IContactAkuiteoSynchronizer, ContactAkuiteoSynchronizer>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IInvoiceReceptionService, InvoiceReceptionService>();
            services.AddScoped<IInvoicePublicationService, InvoicePublicationService>();
            services.AddScoped<IInvoiceContentService, InvoiceContentService>();
            services.AddScoped<IAkuiteoContactSyncOperationService, AkuiteoContactSyncOperationService>();
            services.AddScoped<IMissionService, MissionService>();
            services.AddScoped<IMissionReceptionService, MissionReceptionService>();
            services.AddScoped<IMissionPublicationService, MissionPublicationService>();
            services.AddScoped<IMissionConfirmationService, MissionConfirmationService>();

            return services;
        }
    }
}
