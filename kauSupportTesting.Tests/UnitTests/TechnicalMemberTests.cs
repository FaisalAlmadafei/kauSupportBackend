using System.Data;
using System.Data.Common;
using Dapper;
using kauSupport.Connection;
using kauSupport.Controllers.TechnicalSupport;
using kauSupport.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Dapper;


namespace kauSupport.Tests;


public class TechnicalMemberTests

{
    [Fact]
    public async Task GetReportsByTechnicalMemberId_ReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var userId = "2222222";

        var expectedReports = new List<Report>
        {
            new Report
            {
                reportID = 1,
                deviceNumber = "1",
                serialNumber = "SN-L1-1",
                deviceLocatedLab = "2",
                reportType = "issue",
                reportStatus = "InProgress",
                problemDescription = "PC not working",
                reportedBy = "1111111",
                reportDate = DateTime.Now.Date,
                repairDate = DateTime.Now.Date,
                actionTaken = "No action taken yet...",
                assignedTaskTo = "2222222",
                problemType = "Hardware",
                reportedByFirstName = "Faisal",
                reportedByLastName = "Almadafei",
                assignedToFirstName = "Ali",
                assignedToLastName = "Saud"
            },
        };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedReports);

        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetReportsByTechnicalMemberID(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReports = Assert.IsType<List<Report>>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedReports);
    }
    //------------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetReportsByTechnicalMemberId_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var userId = "2222222";

        var expectedReports = new List<Report>();


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedReports);

        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetReportsByTechnicalMemberID(userId);


        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Reports not found ...", badRequestResult.Value);
    }

    //------------------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task SearchForDevice_ReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var serialNumber = "SN-L1-1";


        var expectedDevice =
            new Device
            {
                serialNumber = "SN-L1-1",
                deviceNumber = 1,
                deviceStatus = "Working",
                type = "PC",
                deviceLocatedLab = "1",
                arrivalDate = new DateTime(2021, 1, 15),
                nextPeriodicDate = new DateTime(2024, 1, 15)
            };
        var expectedReports = new List<Report>
        {
            new Report
            {
                reportID = 1,
                deviceNumber = "1",
                serialNumber = "SN-L1-1",
                deviceLocatedLab = "2",
                reportType = "issue",
                reportStatus = "InProgress",
                problemDescription = "PC not working",
                reportedBy = "1111111",
                reportDate = new DateTime(2021, 1, 15),
                repairDate = new DateTime(2024, 1, 15),
                actionTaken = "No action taken yet...",
                assignedTaskTo = "2222222",
                problemType = "Hardware",
                reportedByFirstName = "Faisal",
                reportedByLastName = "Almadafei",
                assignedToFirstName = "Ali",
                assignedToLastName = "Saud"
            },
        };


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<Device>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedDevice);
        mockConnection.SetupDapperAsync(c => c.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedReports);

        DeviceReports deviceAndReports = new DeviceReports();
        deviceAndReports.device = expectedDevice;
        deviceAndReports.reports = expectedReports;


        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.SearchForDevice(serialNumber);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDeviceAndReports = Assert.IsType<DeviceReports>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedDeviceAndReports);
    }

    //------------------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task SearchForDevice_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var serialNumber = "SN-L1-1";


        var expectedDevice = new Device();
        var expectedReports = new List<Report>();


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<Device>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedDevice);
        mockConnection.SetupDapperAsync(c => c.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedReports);


        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.SearchForDevice(serialNumber);


        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Device not found...", badRequestResult.Value);
    }

    //------------------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task HandelReportReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var reportId = 1;
        var actionTaken = "PC is now working";

        var expectedReport = new Report
        {
            reportID = 1,
            deviceNumber = "1",
            serialNumber = "SN-L1-1",
            deviceLocatedLab = "2",
            reportType = "issue",
            reportStatus = "InProgress",
            problemDescription = "PC not working",
            reportedBy = "1111111",
            reportDate = DateTime.Now.Date,
            repairDate = DateTime.Now.Date,
            actionTaken = "No action taken yet...",
            assignedTaskTo = "2222222",
            problemType = "Hardware",
            reportedByFirstName = "Faisal",
            reportedByLastName = "Almadafei",
            assignedToFirstName = "Ali",
            assignedToLastName = "Saud"
        };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<Report>(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
            .ReturnsAsync(expectedReport);


        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.handelReport(reportId, actionTaken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Report handled successfully!", okResult.Value);
    }


    //------------------------------------------------------------------------------------------------------------------
    [Fact]

    public async Task GetNotificationsReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var expectedNotifications = new List<Notification>
        {
            new Notification
            {
                userId = "2222222",
                NotificationType = "issus",
                reportId = 101,
                notificationId = 5
            },
        };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Notification>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedNotifications);

        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getNotifications();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotifications = Assert.IsType<List<Notification>>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedNotifications);
    }
    //------------------------------------------------------------------------------------------------------------------


    [Fact]

    public async Task GetNotificationsReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var expectedNotifications = new List<Notification>();


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Notification>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedNotifications);

        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getNotifications();

        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No Notifications found ...", badRequestResult.Value);
    }
    //------------------------------------------------------------------------------------------------------------------


    [Fact]

    public async Task GetReportsNotificationsByUserIdReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var notificationsCount = 45;

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(notificationsCount);

        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getReportsNotificationsByUserId("2222222");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotifications = Assert.IsType<int>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedNotifications);
    }
    //------------------------------------------------------------------------------------------------------------------

    [Fact]

    public async Task GetRequestsNotificationsByUserIdReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var notificationsCount = 25;

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(notificationsCount);

        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getReportsNotificationsByUserId("2222222");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedNotifications = Assert.IsType<int>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedNotifications);
    }

    //------------------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task HandelRequestReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var requestId = 101;
        var replay = "I will unblock react.js";
        var requestStatus = "approved";

        var expectedRequest = new List<Service>
        {
            new Service
            {
                RequestID = 101,
                RequestedBy = "1111111",
                RequestStatus = "Pending",
                TechnicalSupportReply = "No replay yet...",
                Request = "Unblock React.js",
                AssignedTo = "3333333",
                requestedByFirstName = "Faisal",
                requestedByLastName = "Fawaz",
                assignedToFirstName = "Ali",
                assignedToLastName = "Saud"


            
        }
         
            };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Service>(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
            .ReturnsAsync(expectedRequest);


        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.handelRequest(requestId, replay, requestStatus);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Request handled successfully", okResult.Value);

    }
    
    //------------------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task HandelRequestReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var requestId = 101;
        var replay = "I will unblock react.js";
        var requestStatus = "approved";
        var exprectedReques = new List<Service>() ;
        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        // Simulate the scenario where the request is not found in the database
        mockConnection.SetupDapperAsync(c => c.QueryAsync<Service>(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
            .ReturnsAsync(exprectedReques);

        var controller = new TechnicalMember_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.handelRequest(requestId, replay, requestStatus);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Could not handel request", badRequestResult.Value);
    }

}