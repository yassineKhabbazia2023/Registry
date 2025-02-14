//// <copyright file="ValidateOperationTests.cs" company="Pulse">
//// Copyright (c) Pulse. All rights reserved.
//// </copyright>

namespace Registry.AzureFuctions.Tests.Extensions
{
    public class PaginatorTest
    {
        [Theory]
        [InlineData(100, 10, 10)]
        [InlineData(10, 10, 1)]
        [InlineData(5, 10, 1)]
        [InlineData(0, 10, 1)]
        [InlineData(30, 25, 2)]
        [InlineData(10, 3, 4)]
        public void GetTotalPages_Valid_ReturnsExpected(int totalItems, int pageSize, int expectedPages)
        {
            // Arrange & Act
            float resultPages = Paginator.GetTotalPages(totalItems, pageSize);

            // Assert
            Assert.Equal(expectedPages, resultPages);
        }

        [Theory]
        [InlineData(5, 5)]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        public void GetValidPageNumber_Valid_ReturnsExpected(int pageNumber, int expectedPageNumber)
        {
            // Act
            int resultPageNumber = Paginator.GetValidPageNumber(pageNumber);

            // Assert
            Assert.Equal(expectedPageNumber, resultPageNumber);
        }

        [Theory]
        [InlineData(10, 10)]
        [InlineData(0, int.MaxValue)]
        [InlineData(-10, int.MaxValue)]
        public void GetValidPageSize_Valid_ReturnsExpected(int pageSize, int expectedPageSize)
        {
            // Act
            int resultPageSize = Paginator.GetValidPageSize(pageSize);

            // Assert
            Assert.Equal(expectedPageSize, resultPageSize);
        }
    }
}
