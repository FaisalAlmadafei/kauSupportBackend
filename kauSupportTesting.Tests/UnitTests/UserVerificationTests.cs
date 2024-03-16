using System.Data;
using Dapper;
using kauSupport.Connection;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Controllers.UserVerification;
using kauSupport.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
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
       this.controller = new UserVerification_Controller(mockConnectionFactory.Object );

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
        public async Task LogIn_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockConnection = new Mock<IDbConnection>();
            var mockController = new Mock<UserVerification_Controller>(mockConnectionFactory.Object);

            var expectedUser = new User
            {
                UserId = "testUser",
                firstName = "Test",
                lastName = "User",
                role = "User",
                email = "test@example.com"
            };

            var password = "1234567";
            var hashedPassword = "CfDJ8HMyWVpqzPFGpvrTZ6Xvwrx0F3uHjiXL2T12BOJHJ4SQ6X5cKUH97Ms7wYTvtweG7PP0KqoQfgJ5VPbz5QEQuYQBh6hsCrnQPZxUf6ZE5SqdIVfsI-TMPPsjsK84vYp1yQ";

            mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedUser);
            mockConnection.SetupDapperAsync(c => c.QueryAsync<string>(
                    It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(new List<string> { hashedPassword });
            
            // Set up the behavior of checkPassword method indirectly by mocking the behavior of methods that use it
            mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await mockController.Object.LogIn("1111111", password);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<User>(okResult.Value);

            // Additional assertions if needed
            Assert.Equal(expectedUser.UserId, returnedUser.UserId);
            Assert.Equal(expectedUser.firstName, returnedUser.firstName);
            Assert.Equal(expectedUser.lastName, returnedUser.lastName);
            Assert.Equal(expectedUser.role, returnedUser.role);
            Assert.Equal(expectedUser.email, returnedUser.email);
        }
    }
   

