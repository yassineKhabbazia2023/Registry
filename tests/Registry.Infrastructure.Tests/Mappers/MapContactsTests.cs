// <copyright file="MapContactsTests.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Models;
using AutoFixture;
using Application.Mappers;

namespace Registry.Infrastructure.Tests.Mappers;

public class MapContactsTests
{
    private readonly Fixture _fixture;

    public MapContactsTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void MapContactCsvToContactEntity_ShouldMapsCorrectly()
    {
        var contact = _fixture.Create<RefContactCsv>();

        var result = contact.MapContactCsvToContactEntity();

        Assert.NotNull(result);
        Assert.Equal(contact.ContactFlagStatus, result.ContactFlagStatus);
        Assert.Equal(contact.Email, result.Email);
        Assert.Equal(contact.FirstName, result.FirstName);
        Assert.Equal(contact.LastName, result.LastName);
        Assert.Equal(contact.IsCustomer, result.IsCustomer);
        Assert.Equal(contact.LandPhone, result.LandPhone);
        Assert.Equal(contact.MobilePhone, result.MobilePhone);
        Assert.Equal(contact.JobDescription, result.JobDescription);
        Assert.Equal(contact.OfficeId, result.OfficeCode);
        Assert.Equal(contact.Operation, result.OperationType);
    }

    [Fact]
    public void MapContactCsvToContactEntity_WithNullSource_ShouldReturnNull()
    {
        var result = MapContacts.MapContactCsvToContactEntity(null!);

        Assert.Null(result);
    }

    [Fact]
    public void MapContactCsvsToContactEntities_ShouldMapsCorrectly()
    {
        var contacts = _fixture.CreateMany<RefContactCsv>(2);

        var result = contacts.MapContactCsvsToContactEntities();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(contacts.Count(), result.Count());
    }

    [Fact]
    public void MapContactCsvsToContactEntities_WithNullSource_ShouldReturnEmptyList()
    {
        var result = MapContacts.MapContactCsvsToContactEntities(null!);

        Assert.Empty(result);
    }
}
