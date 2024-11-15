using Application.Interfaces;
using Application.Models;
using Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Providers
{
    public class ContactRegistryProvider : IContactRegistryProvider
    {
        private readonly IHttpClientFactory factory;

        public ContactRegistryProvider(IHttpClientFactory factory)
        {
            this.factory = factory;
        }

        public async Task<HttpResponseMessage> CreateContactAsync(ContactRegistry contactRegistry)
        {
            var url = "/contacts";
            var json = JsonConvert.SerializeObject(contactRegistry, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            var httpClient = factory.CreateClient("RegistryApi");
            var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

            return await httpClient.PostAsync(url, content);
        }

        public async Task<HttpResponseMessage> UpdateContactAsync(ContactRegistry contactRegistry)
        {
                var url = "/contacts/pulse";
                var json = JsonConvert.SerializeObject(contactRegistry, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                var httpClient = factory.CreateClient("RegistryApi");
                var content = new StringContent(json, encoding: Encoding.UTF8, mediaType: "application/json");

                return await httpClient.PutAsync(url, content);
        }

    }
}
