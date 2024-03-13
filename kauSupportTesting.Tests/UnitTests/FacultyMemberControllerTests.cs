using Moq;
using Moq.Dapper;
using Xunit;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Core;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using kauSupport.Connection;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        [Fact]
        public async Task GetMyReports_ReturnsBadRequest()
        {
            // Arrange---------------------------------------------
            var UserId = "1111111";
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            var emptyReports = new List<Report>();


            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);
            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Report>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(emptyReports);

            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);


            // Act--------------------------------------------------------
            var result = await controller.getMyReports(UserId);

            // Assert----------------------------------------------------------
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("You have not reported any device yet...", badRequestResult.Value);
        }

        [Fact]
        public async Task GetLabsWithDeviceCounts_ReturnsOk()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            var expectedLabs = new List<Lab>
            {
                new Lab
                {
                    labNumber = "1",
                    labCapacity = 25,
                    labLocation = "Building 31"
                },
                new Lab
                {
                    labNumber = "2",
                    labCapacity = 25,
                    labLocation = "Building 31"
                },
            };

            var expectedReportedCount = 5;
            var expectedWorkingCount = 20;

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Lab>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedLabs);

            mockDbConnection
                .SetupDapperAsync(conn =>
                    conn.QuerySingleAsync<int>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(expectedReportedCount);

            mockDbConnection
                .SetupDapperAsync(conn =>
                    conn.QuerySingleAsync<int>(It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(expectedWorkingCount);


            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.GetLabsWithDeviceCounts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            var labsWithDeviceCountsList = Assert.IsType<List<LabWithDeviceCounts>>(okResult.Value);

            // Check that the returned collection is not empty
            Assert.NotEmpty(labsWithDeviceCountsList);
        }

        [Fact]
        public async Task GetLabsWithDeviceCounts_ReturnsBadRequest()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            var emptyLabs = new List<Lab>();


            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Lab>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(emptyLabs);


            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.GetLabsWithDeviceCounts();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No Labs found", badRequestResult.Value);
        }


        [Fact]
        public async Task RequestService_ReturnsOk()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            // Set up a mock supervisor and faculty member
            var mockSupervisor = new User
            {
                UserId = "3333333", firstName = "Saud", lastName = "Faisal", role = "Supervisor",
                email = "test@hotmail.com"
            };
            var mockFacultyMember = new User
            {
                UserId = "1111111", firstName = "Ali", lastName = "Saad", role = "Faculty Member",
                email = "test@hotmail.com"
            };
            var requestId = 101;
            mockDbConnection
                .SetupDapperAsync(conn =>
                    conn.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(mockSupervisor); // Mock supervisor query


            mockDbConnection
                .SetupDapperAsync(conn =>
                    conn.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(mockFacultyMember); // Mock faculty member query


            mockDbConnection
                .SetupDapperAsync(conn => conn.QuerySingleAsync<int>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(requestId); // Mock Request_Id

            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.RequestService("Unblock React.js", mockFacultyMember.UserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Request added successfully", okResult.Value);
        }

        [Fact]
        public async Task RequestService_ReturnsBadRequest()
        {
            // Arrange
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

            // Set up a mock supervisor and faculty member
            var mockSupervisor = new User
            {
                UserId = "3333333", firstName = "Saud", lastName = "Faisal", role = "Supervisor",
                email = "test@hotmail.com"
            };
            var mockFacultyMember = new User
            {
                UserId = "1111111", firstName = "Ali", lastName = "Saad", role = "Faculty Member",
                email = "test@hotmail.com"
            };
            var requestId = 0;
            mockDbConnection
                .SetupDapperAsync(conn =>
                    conn.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(mockSupervisor); // Mock supervisor query


            mockDbConnection
                .SetupDapperAsync(conn =>
                    conn.QueryFirstOrDefaultAsync<User>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(mockFacultyMember); // Mock faculty member query


            mockDbConnection
                .SetupDapperAsync(conn => conn.QuerySingleAsync<int>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(requestId); // Mock Request_Id

            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act
            var result = await controller.RequestService("Unblock React.js", mockFacultyMember.UserId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Could not add request", badRequestResult.Value);
        }

        [Fact]
        public async Task GetMyRequests_ReturnsOk()
        {
            // Arrange---------------------------------------------
            var UserId = "1111111";
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            var expectedRequests = new List<Service>
            {
                new Service
                {
                    RequestID = 101,
                    RequestedBy = "1111111",
                    RequestStatus = "Approved",
                    TechnicalSupportReply = "React.js will be unblocked",
                    Request = "Unblock React.js",
                    AssignedTo = "3333333",
                    requestedByFirstName = "Faisal",
                    requestedByLastName = "Fawaz",
                    assignedToFirstName = "Ali",
                    assignedToLastName = "Saud"

                },
            };

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);
            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Service>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedRequests);


            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act--------------------------------------------------------
            var result = await controller.getMyRequests(UserId);

            // Assert----------------------------------------------------------
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedRequests = Assert.IsType<List<Service>>(okResult.Value);

            // Check that the returned collection is not empty
            Assert.NotEmpty(returnedRequests);
        }

        public async Task GetMyRequests_ReturnsBadRequests()
        {
            // Arrange---------------------------------------------
            var UserId = "1111111";
            var mockConnectionFactory = new Mock<IDbConnectionFactory>();
            var mockDbConnection = new Mock<IDbConnection>();

            var expectedRequests = new List<Service>();

            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);
            mockDbConnection
                .SetupDapperAsync(conn => conn.QueryAsync<Service>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(expectedRequests);


            var controller = new FacultyMember_Controller(mockConnectionFactory.Object);

            // Act--------------------------------------------------------
            var result = await controller.getMyRequests(UserId);

            // Assert----------------------------------------------------------
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("You have not requested any service yet...", badRequestResult.Value);
        }
        
        
      /*  [Fact]
        public async Task AddReport_And_CheckPreviousReports_ShouldReturnCorrectResults()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Pass the configuration object to SqlConnectionFactory
            var _dbConnectionFactory = new SqlConnectionFactory(configuration);

            var controller = new FacultyMember_Controller(_dbConnectionFactory);

            // Sample report data
            var reportData = new
            {
                Device_Number = "3",
                Serial_Number = "SN-L1-1",
                Device_LocatedLab = "1",
                Problem_Description = "PC not working integration testing",
                Reported_By = "1111111", // Assuming a valid user ID
                Problem_Type = "Hardware"
            };

            // Act: Add a report
            await controller.addReport(
                reportData.Device_Number,
                reportData.Serial_Number,
                reportData.Device_LocatedLab,
                reportData.Problem_Description,
                reportData.Reported_By,
                reportData.Problem_Type);

            // Act: Get previous reports
            var getResult = await controller.getMyReports(reportData.Reported_By);
            var okObjectResult = getResult as OkObjectResult;
            var reportList = okObjectResult.Value as List<Report>;
        
            var addedReport = reportList.FirstOrDefault(r =>
                r.deviceNumber == reportData.Device_Number &&
                r.serialNumber == reportData.Serial_Number &&
                r.deviceLocatedLab == reportData.Device_LocatedLab &&
                r.problemDescription == reportData.Problem_Description &&
                r.reportedBy == reportData.Reported_By &&
                r.problemType == reportData.Problem_Type);

            Assert.NotNull(addedReport);

         
        }
*/
    }
    


}