using System.Data;
using Dapper;
using kauSupport.Connection;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Controllers.UserVerification;
using kauSupport.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Dapper;

namespace kauSupport.Tests;

public class UserVerificationTests
{
  
    //------------------------------------------------------------------------------------------------------------------
    [Fact]

    public async Task GetTechnicalMembersReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var expectedUsers = new List<User>
        {
            new User
            {
              UserId = "2222222" ,
              firstName = "Ali" ,
              lastName = "Saud" ,
              role = "Technical Member" ,
              email = "test@hotmail.com"
            },
        };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<User>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedUsers);

        var controller = new UserVerification_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetTechnicalMembers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUsers = Assert.IsType<List<User>>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedUsers);
    }
    //------------------------------------------------------------------------------------------------------------------
    [Fact]

    public async Task GetTechnicalMembersReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var expectedUsers = new List<User>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<User>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedUsers);

        var controller = new UserVerification_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetTechnicalMembers();

        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No users found..", badRequestResult.Value);
    }
    
    //------------------------------------------------------------------------------------------------------------------
   
}