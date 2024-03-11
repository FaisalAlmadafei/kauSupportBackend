using Moq;
using Moq.Dapper;
using Xunit;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using kauSupport.Connection;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Models;

namespace kauSupport.Tests
{
    public class FacultyMemberControllerTests
    {
        [Fact]
        public async Task GetLabs_ReturnsOk()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            // Use any valid lab data for testing the structure
            var expectedLabs = new List<Lab>
            {
                new Lab { labNumber = "1", labCapacity = 25, labLocation = "Building 31" },
                // ... add more labs as needed for your test
            };

            mockDbConnection.SetupDapperAsync(conn => conn.QueryAsync<Lab>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedLabs);

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);


            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.getLabs();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedLabs = Assert.IsType<List<Lab>>(okResult.Value);

            // Check that the returned collection is not empty
            Assert.NotEmpty(returnedLabs);
        }

        [Fact]
        public async Task GetLabs_ReturnsBadRequest()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            // Use an empty list to simulate no labs returned
            var emptyLabs = new List<Lab>();

            mockDbConnection.SetupDapperAsync(conn => conn.QueryAsync<Lab>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(emptyLabs);

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);


            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.getLabs();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No Labs found...", badRequestResult.Value);
        }

        [Fact]
        public async Task GetLabDevices_ReturnsOk()
        {
            // Arrange
            var Lab_Number = "1";
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            var expectedDevices = new List<Device>
            {
                new Device
                {
                    serialNumber = "SN-L1-1",
                    deviceNumber = 1,
                    deviceStatus = "Reported",
                    type = "Smart Board",
                    deviceLocatedLab = "1",
                    arrivalDate = DateTime.Parse("2024-03-06"),
                    nextPeriodicDate = DateTime.Parse("2024-09-06")
                },
            };

            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Device>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedDevices);

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.getLabDevices(Lab_Number);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedDevices = Assert.IsType<List<Device>>(okResult.Value);

            // Check that the returned collection is not empty
            Assert.NotEmpty(returnedDevices);
        }

        [Fact]
        public async Task GetLabDevices_ReturnsBadRequest()
        {
            // Arrange
            var Lab_Number = "10";
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            // Use an empty list to simulate no devices returned
            var emptyDevices = new List<Device>();

            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Device>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(emptyDevices);

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.getLabDevices(Lab_Number);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No devices found in this Lab...", badRequestResult.Value);
        }

        [Fact]
        public async Task AddReport_ReturnsOk()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();
            var expectedUser = new User
                { UserId = "1234567", firstName = "Faisal", lastName = "Saud", email = "test@hotmail.com" };
            var expectedReportId = 1;
            
            mockDbConnection.SetupDapperAsync(conn => conn.QueryFirstOrDefaultAsync<User>(
                    It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedUser);

            mockDbConnection.SetupDapperAsync(conn => conn.QuerySingleAsync<int>(
                    It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedReportId);

            // Setup the mock connection factory to return the mock connection
            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);


            // Act
            var result = await controller.addReport(
                Device_Number: "1",
                Serial_Number: "SN-L1-1",
                Device_LocatedLab: "1",
                Problem_Description: "Pc not working",
                Reported_By: "1111111",
                Problem_Type: "Hardware");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Report added successfully", okResult.Value);
        }
        [Fact]
        public async Task GetMyReports_ReturnsOk()
        {
            // Arrange---------------------------------------------
            var UserId = "1111111";
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            var expectedReports = new List<Report>
            {
               
                
                
                new Report
                {
                    reportID = 1,
                    deviceNumber = "1",
                    serialNumber = "SN-L1-1",
                    deviceLocatedLab = "1",
                    reportType = "issue",
                    reportStatus = "In Progress",
                    problemDescription = "PC not working",
                    reportedBy = "1111111",
                    reportDate = DateTime.Now.Date, // Some past report date
                    repairDate = DateTime.Now.Date, // Some future repair date
                    actionTaken = "No Action taken yet...",
                    assignedTaskTo = "2222222",
                    problemType = "Hardware",
                    reportedByFirstName = "Faisal",
                    reportedByLastName = "Saud",
                    assignedToFirstName = "Saad",
                    assignedToLastName = "Almadafei"
                },
             
            };


            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Report>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedReports);

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act--------------------------------------------------------
            var result = await controller.getMyReports(UserId);

            // Assert----------------------------------------------------------
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReports = Assert.IsType<List<Report>>(okResult.Value);

            // Check that the returned collection is not empty
            Assert.NotEmpty(returnedReports);
        }
    }
}