using System.Data;
using Dapper;
using kauSupport.Connection;
using kauSupport.Controllers.TechnicalSupport;
using kauSupport.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Dapper;


namespace kauSupport.Tests;

public class TechnicalSupervisorTests
{
    
    [Fact]
    public async Task GetReports_ReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

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

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getReports();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReports = Assert.IsType<List<Report>>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedReports);
    }

    [Fact]
    public async Task GetReports_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var expectedReports = new List<Report>();


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedReports);

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getReports();


        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No reports found...", badRequestResult.Value);
    }
     [Fact]
    public async Task AssignReport_ReturnsOk()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        
        // Act
        var assignResult = await technicalSupervisorController.AssignReport("2222222", 2453);
        
        //Assert
            var okAssignResult=  Assert.IsType<OkObjectResult>(assignResult);
            Assert.Equal("Report assigned successfully.", okAssignResult.Value);

        

    }

    [Fact]
    public async Task AssignReport_ReturnsBadRequest()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        
        // Act
        var assignResult = await technicalSupervisorController.AssignReport("2222222", -1);
        
        //Assert
        var badRequestObject=  Assert.IsType<BadRequestObjectResult>(assignResult);
        Assert.Equal("Could not assign Report.", badRequestObject.Value);
    }


    [Fact]
    public async Task GetNewReportsForSupervisor_ReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var reportID = 101;

        var devices = new List<Device>
        {
            new Device
            {
                serialNumber = "SN-L1-1",
                deviceNumber = 1,
                deviceStatus = "Working",
                type = "PC",
                deviceLocatedLab = "1",
                arrivalDate = new DateTime(2021, 1, 15),
                nextPeriodicDate = new DateTime(2024, 1, 15)
            },
        };

        var supervisor = new User
        {
            UserId = "3333333", firstName = "Ail", lastName = "Saud", role = "Supervisor", email = "test@hotmail.com"
        };

        var reports = new List<Report>
        {
            new Report
            {
                reportID = reportID,
                deviceNumber = "1",
                serialNumber = "SN-L1-1",
                deviceLocatedLab = "1",
                reportType = "Periodic maintenance",
                reportStatus = "Pending",
                problemDescription = "The device needs a Periodic maintenance",
                reportedBy = "1111111",
                reportDate = DateTime.Now.Date,
                repairDate = DateTime.Now.Date,
                actionTaken = "No Action taken yet...",
                assignedTaskTo = "3333333",
                problemType = "Periodic maintenance",
                reportedByFirstName = "KauSupport",
                reportedByLastName = "System",
                assignedToFirstName = "Ali",
                assignedToLastName = "Saud"
            },
        };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(conn => conn.QueryAsync<Device>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(devices);

        mockConnection.SetupDapperAsync(conn => conn.QueryFirstOrDefaultAsync<User>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(supervisor);
        
        foreach (var device in devices)
        {
            reportID = reportID + 1;


            mockConnection.SetupDapperAsync(conn => conn.QuerySingleAsync<int>(
                    It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(reportID);
        }


        mockConnection.SetupDapperAsync(conn => conn.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(reports);

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetNewReportsForSupervisor();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReports = Assert.IsType<List<Report>>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedReports);
    }

    [Fact]
    public async Task GetNewReportsForSupervisor_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var reportID = 101;

        var devices = new List<Device>
        {
            new Device
            {
                serialNumber = "SN-L1-1",
                deviceNumber = 1,
                deviceStatus = "Working",
                type = "PC",
                deviceLocatedLab = "1",
                arrivalDate = new DateTime(2021, 1, 15),
                nextPeriodicDate = new DateTime(2024, 1, 15)
            },
        };

        var supervisor = new User
        {
            UserId = "3333333", firstName = "Ail", lastName = "Saud", role = "Supervisor", email = "test@hotmail.com"
        };

        var reports = new List<Report>();


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(conn => conn.QueryAsync<Device>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(devices);

        mockConnection.SetupDapperAsync(conn => conn.QueryFirstOrDefaultAsync<User>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(supervisor);
        foreach (var device in devices)
        {
            reportID = reportID + 1;


            mockConnection.SetupDapperAsync(conn => conn.QuerySingleAsync<int>(
                    It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(reportID);
        }


        mockConnection.SetupDapperAsync(conn => conn.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(reports);

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetNewReportsForSupervisor();

        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Reports not found ...", badRequestResult.Value);
    }

    [Fact]
    public async Task GetRequestById_ReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var userId = "3333333";

        var expectedRequests = new List<Service>
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
            },
        };

        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Service>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedRequests);

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetNewRequestByUserId(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedRequests = Assert.IsType<List<Service>>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedRequests);
    }

    [Fact]
    public async Task GetRequestById_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var userId = "3333333";

        var expectedRequests = new List<Service>();


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Service>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedRequests);

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetNewRequestByUserId(userId);

        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No requests fond...", badRequestResult.Value);
    }

    [Fact]
    public async Task MonitorReports_ReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

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

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getReports();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedReports = Assert.IsType<List<Report>>(okResult.Value);

        // Check if the returned list is not null
        Assert.NotNull(returnedReports);
    }

    [Fact]
    public async Task MonitorReports_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();

        var expectedReports = new List<Report>();


        mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);

        mockConnection.SetupDapperAsync(c => c.QueryAsync<Report>(
                It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedReports);

        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getReports();

        // Assert----------------------------------------------------------
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No reports found...", badRequestResult.Value);
    }
    
    
    [Fact]
    public async Task AssignRequest_ReturnsOk()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        
        //Act 
        var assignResult = await technicalSupervisorController.AssignRequest("2222222", 89);
      
        // Assert
        var okAssignResult=  Assert.IsType<OkObjectResult>(assignResult);
        Assert.Equal("Request assigned successfully", okAssignResult.Value);
    }
    [Fact]
    public async Task AssignRequest_ReturnsBadRequest()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        
        //Act 
        var assignResult = await technicalSupervisorController.AssignRequest("2222222", -1);
      
        // Assert
        var badRequestObject=  Assert.IsType<BadRequestObjectResult>(assignResult);
        Assert.Equal("Request could not be assigned..", badRequestObject.Value);
     

    }
    [Fact]
    public async Task GetDevices_ReturnsOk()
    {
        // Arrange
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
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

        mockDbConnection.SetupDapperAsync(conn => conn.QueryAsync<Device>(It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedDevices);



        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getDevices();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDevices = Assert.IsType<List<Device>>(okResult.Value);

        // Check that the returned collection is not empty
        Assert.NotEmpty(returnedDevices);
    }
    
    [Fact]
    public async Task GetDevices_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockDbConnection = new Mock<IDbConnection>();


        var expectedDevices = new List<Device>();
     
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

        mockDbConnection.SetupDapperAsync(conn => conn.QueryAsync<Device>(It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedDevices);



        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.getDevices();
        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No devices found...", badRequestResult.Value);

      
    }
    
    [Fact]
    public async Task GetTeamProgress_ReturnsOk()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockDbConnection = new Mock<IDbConnection>();
        var reportsAssignedCount = 30;

        var teamProgress = new List<TeamMembersProgress>();

        var expectedTeam = new List<User>
        {
            new User
            {
                UserId = "1111111", firstName = "Faisal", lastName = "Saad", role = "Technical Member",
                email = "test@hotmail.com"
            }

        };
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

        mockDbConnection.SetupDapperAsync(conn => conn.QueryAsync<User>(It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedTeam);

        foreach (var member in expectedTeam)
        {
            mockDbConnection.SetupDapperAsync(conn => conn.QuerySingleAsync<int>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(reportsAssignedCount);
            teamProgress.Add(new TeamMembersProgress
            {
                firstName = member.firstName,
                lastName = member.lastName,
                numberOfReports = reportsAssignedCount
            });
        }
        


        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetTeamProgress();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTeamProgress = Assert.IsType<List<TeamMembersProgress>>(okResult.Value);

        // Check that the returned collection is not empty
        Assert.NotEmpty(returnedTeamProgress);
    }
    
    [Fact]
    public async Task GetTeamProgress_ReturnsBadRequest()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockDbConnection = new Mock<IDbConnection>();
        var reportsAssignedCount = 30;

        var teamProgress = new List<TeamMembersProgress>();

        var expectedTeam = new List<User>();
      
        mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockDbConnection.Object);

        mockDbConnection.SetupDapperAsync(conn => conn.QueryAsync<User>(It.IsAny<string>(), null, null, null, null))
            .ReturnsAsync(expectedTeam);

        foreach (var member in expectedTeam)
        {
            mockDbConnection.SetupDapperAsync(conn => conn.QuerySingleAsync<int>(It.IsAny<string>(), null, null, null, null))
                .ReturnsAsync(reportsAssignedCount);
            teamProgress.Add(new TeamMembersProgress
            {
                firstName = member.firstName,
                lastName = member.lastName,
                numberOfReports = reportsAssignedCount
            });
        }
        


        var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

        // Act
        var result = await controller.GetTeamProgress();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Could not gather data", badRequestResult.Value);
    }
    [Fact]
    public async Task GetReportStatistics_ReturnsOk()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        
        //Act
        var reportStatisticsResult = await technicalSupervisorController.GetReportStatistics();
       
        //Assert
        var okResult = Assert.IsType<OkObjectResult>(reportStatisticsResult);
        var  statisticsResult = Assert.IsAssignableFrom<TotalReportSummary>(okResult.Value);
        Assert.NotNull(statisticsResult); 
       
       
    }
    [Fact]
    public async Task GetDeviceReportStatistics_ReturnsOk()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        
        //Act
        var reportStatisticsResult = await technicalSupervisorController.GetDeviceReportStatistics("SN-L1-2");
       
        //Assert
        var okResult = Assert.IsType<OkObjectResult>(reportStatisticsResult);
        var  statisticsResult = Assert.IsAssignableFrom<TotalReportSummary>(okResult.Value);
        Assert.NotNull(statisticsResult); 
       
       
    }
    [Fact]
    public async Task GetDeviceReportStatistics_ReturnsBadRequest()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        var _dbConnectionFactory = new SqlConnectionFactory(configuration);
        var technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        
        //Act
        var reportStatisticsResult = await technicalSupervisorController.GetDeviceReportStatistics("-1");
       
        //Assert
        var badRequestObjectResult  = Assert.IsType<BadRequestObjectResult>(reportStatisticsResult);
        Assert.Equal("No Reports or Device Found...", badRequestObjectResult.Value);

         
       
    }

   [Fact]
   public async Task GetDevicesStatistics_ReturnsOk()
   {
       // Arrange
       var mockConnectionFactory = new Mock<IDbConnectionFactory>();
       var mockConnection = new Mock<IDbConnection>();
       var DevicesCount = 50;
       var DevicesWorkingCount = 40;
       var DevicesReportedCount  = 10;
       var devicesSummary = new DeviceSummary
       {
           totalDevicesCount = DevicesCount,
           workingDevicesCount = DevicesWorkingCount,
           NotWorkingDevicesCount = DevicesReportedCount
       };

       mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);
       mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>( It.IsAny<string>(), null, null, null, null))
           .ReturnsAsync(DevicesCount); 
       mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>( It.IsAny<string>(), null, null, null, null))
           .ReturnsAsync(DevicesWorkingCount); 
       mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>( It.IsAny<string>(), null, null, null, null))
           .ReturnsAsync(DevicesReportedCount); 



       var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

       // Act
       var result = await controller.GetDevicesStatistics();

       // Assert
       var okResult = Assert.IsType<OkObjectResult>(result);
       var deviceSummaryResult = Assert.IsAssignableFrom<DeviceSummary>(okResult.Value);
       Assert.NotNull(deviceSummaryResult); 
   }
   
   [Fact]
   public async Task GetDevicesStatistics_ReturnsBadRequest()
   {
       // Arrange
       var mockConnectionFactory = new Mock<IDbConnectionFactory>();
       var mockConnection = new Mock<IDbConnection>();
       var DevicesCount = 0;
       var DevicesWorkingCount = 0;
       var DevicesReportedCount  = 0;
       var devicesSummary = new DeviceSummary
       {
           totalDevicesCount = DevicesCount,
           workingDevicesCount = DevicesWorkingCount,
           NotWorkingDevicesCount = DevicesReportedCount
       };

       mockConnectionFactory.Setup(x => x.CreateConnection()).Returns(mockConnection.Object);
       mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>( It.IsAny<string>(), null, null, null, null))
           .ReturnsAsync(DevicesCount); 
       mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>( It.IsAny<string>(), null, null, null, null))
           .ReturnsAsync(DevicesWorkingCount); 
       mockConnection.SetupDapperAsync(c => c.QuerySingleAsync<int>( It.IsAny<string>(), null, null, null, null))
           .ReturnsAsync(DevicesReportedCount); 



       var controller = new TechnicalSupervisor_Controller(mockConnectionFactory.Object);

       // Act
       var result = await controller.GetDevicesStatistics();

       // Assert
       var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
       Assert.Equal("No Devices Found...", badRequestResult.Value);
   }
      
  
}
    
    
    
