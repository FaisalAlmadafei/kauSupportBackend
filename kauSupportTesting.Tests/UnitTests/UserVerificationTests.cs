using System.Data;
using Dapper;
using kauSupport.Connection;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Controllers.TechnicalSupport;
using kauSupport.Controllers.UserVerification;
using kauSupport.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Dapper;

namespace kauSupport.Tests;

public class UserVerificationTests
{
    private Mock<IDbConnectionFactory> mockConnectionFactory;
    private Mock<IDbConnection> mockConnection;
    private UserVerification_Controller controller;


    public UserVerificationTests()
    {
        this.mockConnectionFactory = new Mock<IDbConnectionFactory>();
        this.mockConnection = new Mock<IDbConnection>();
        this.controller = new UserVerification_Controller(mockConnectionFactory.Object);
    }

    //------------------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task GetTechnicalMembersReturnsOk()
    {
        // Arrange


        var expectedUsers = new List<User>
        {
            new User
            {
                UserId = "2222222",
                firstName = "Ali",
                lastName = "Saud",
                role = "Technical Member",
                email = "test@hotmail.com"
            },
        };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<User>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedUsers);


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

        var expectedUsers = new List<User>();

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<User>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedUsers);


        // Act
        var result = await controller.GetTechnicalMembers();

        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No users found..", badRequestResult.Value);
    }

    //------------------------------------------------------------------------------------------------------------------
    //------------------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task LogIn_ReturnsOk()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var userVerificationController = new UserVerification_Controller(_dbConnectionFactory);

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);

        //Assert
        Assert.IsType<OkObjectResult>(loginResult);
    }

    [Fact]
    public async Task LogIn_ReturnsBadRequest()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var userVerificationController = new UserVerification_Controller(_dbConnectionFactory);

        var userCredentials = new
        {
            User_Id = "-1",
            Password = "-1"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);

        //Assert
        Assert.IsType<BadRequestObjectResult>(loginResult);
    }
}