// <copyright file="MissionControllerTest.cs" company="Pulse">
// Copyright (c) Pulse. All rights reserved.
// </copyright>

using Application.Interfaces;
using Application.Models.Results;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Registry.WebApi.Controllers;
using System.Net;
using WebApi.Configurations.Models;

namespace Registry.WebApi.Tests.Controllers;

public class MissionControllerTest
{
    private const string ValidToken = "toto";
    private const string CsvData = "AccountNumber;EngagementCode;OfferCode;ProductCode;StartDate;EndDate;Operations";

    private readonly Mock<IMissionReceptionService> _missionReceptionServiceMock;
    private readonly MissionController _sut;

    public MissionControllerTest()
    {
        _missionReceptionServiceMock = new Mock<IMissionReceptionService>(MockBehavior.Strict);
        var options = new Mock<IOptions<TokenModel>>();
        options.Setup(x => x.Value).Returns(new TokenModel { Token = ValidToken });
        _sut = new MissionController(_missionReceptionServiceMock.Object, options.Object);
    }

    public static TheoryData<string> TokenData =>
        new()
        {
            null!,
            string.Empty,
            "                        ",
            "titi",
        };

    [Theory]
    [MemberData(nameof(TokenData))]
    public async Task UpdateAsync_WithWrongToken_ReturnsUnauthorizedWithoutReceiving(string token)
    {
        // Act
        var response = await _sut.UpdateAsync(token, CsvData) as UnauthorizedObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        response.Value.Should().Be("Invalid token.");
        _missionReceptionServiceMock.Verify(x => x.ReceiveAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenReceptionIsRejected_ReturnsBadRequestWithTheError()
    {
        // Arrange
        _missionReceptionServiceMock
            .Setup(x => x.ReceiveAsync(CsvData))
            .ReturnsAsync(MissionCsvReceptionOutcome.Rejected("Invalid data: Missing columns in header"));

        // Act
        var response = await _sut.UpdateAsync(ValidToken, CsvData) as BadRequestObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Value.Should().Be("Invalid data: Missing columns in header");
    }

    [Fact]
    public async Task UpdateAsync_WhenReceptionIsAccepted_ReturnsOkWithTheSummary()
    {
        // Arrange
        var summary = new MissionCsvReceptionResult { AcceptedLines = 2, RejectedLines = 1 };
        _missionReceptionServiceMock
            .Setup(x => x.ReceiveAsync(CsvData))
            .ReturnsAsync(MissionCsvReceptionOutcome.Accepted(summary));

        // Act
        var response = await _sut.UpdateAsync(ValidToken, CsvData) as OkObjectResult;

        // Assert
        response.Should().NotBeNull();
        response!.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response.Value.Should().BeSameAs(summary);
        _missionReceptionServiceMock.Verify(x => x.ReceiveAsync(CsvData), Times.Once);
    }
}
