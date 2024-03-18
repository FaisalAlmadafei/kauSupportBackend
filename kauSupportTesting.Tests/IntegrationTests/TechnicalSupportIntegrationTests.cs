using kauSupport.Controllers.TechnicalSupport;
using kauSupport.Controllers.UserVerification;

namespace kauSupportTesting.Tests.IntegrationTests;

using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using kauSupport.Connection;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Models;

public class TechnicalSupportIntegrationTests
{
    private readonly IConfiguration configuration;
    private readonly TechnicalSupervisor_Controller technicalSupervisorController;
    private readonly TechnicalMember_Controller technicalMemberrController;
    private readonly UserVerification_Controller userVerificationController;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly FacultyMember_Controller facultyMemberController;


    public TechnicalSupportIntegrationTests()
    {
        this.configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        _dbConnectionFactory = new SqlConnectionFactory(configuration);
        technicalSupervisorController = new TechnicalSupervisor_Controller(_dbConnectionFactory);
        technicalMemberrController = new TechnicalMember_Controller(_dbConnectionFactory);
        facultyMemberController = new FacultyMember_Controller(_dbConnectionFactory);
        userVerificationController = new UserVerification_Controller(_dbConnectionFactory);
    }

    [Fact]
    public async Task Login_CheckReports_AssignReport()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);

        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);

            // Act: Get all new reports 
            var getResult = await technicalSupervisorController.GetNewReportsForSupervisor();
            if (getResult is OkObjectResult okObjectResult)
            {
                Assert.IsType<OkObjectResult>(getResult);
                // Act: Assign report 
                var assignResult = await technicalSupervisorController.AssignReport("2222222", 2453);

                if (assignResult is OkObjectResult okAssignResult)
                {
                    Assert.Equal("Report assigned successfully.", okAssignResult.Value);

                    // Act: Get reports by userId
                    var reports = await technicalMemberrController.GetReportsByTechnicalMemberID("2222222");

                    var reportsOkObjectResult = reports as OkObjectResult;
                    var reportsList = reportsOkObjectResult.Value as List<Report>;
                    var assignedReport = reportsList.FirstOrDefault(r =>
                        r.reportID == 2453 &&
                        r.assignedTaskTo == "2222222");

                    Assert.NotNull(assignedReport);
                }
                else if (assignResult is BadRequestObjectResult badAssignResult)
                {
                    Assert.Equal("Could not assign Report.", badAssignResult.Value);
                }
                else
                {
                    Assert.Equal("Fail", "Pass");
                }
            }
            else if (getResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("Reports not found ...", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }

    [Fact]
    public async Task Login_CheckReports_HandleReport()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: Get all new reports 
            var getResult = await technicalSupervisorController.GetNewReportsForSupervisor();
            if (getResult is OkObjectResult)
            {
                Assert.IsType<OkObjectResult>(getResult);
                // Act: Handle report 
                var handleResult =
                    await technicalMemberrController.handelReport(2453, "Integration Testing is working fine2 ....");

                if (handleResult is OkObjectResult okAssignResult)
                {
                    // Check if the message is as expected
                    Assert.Equal("Report handled successfully!", okAssignResult.Value);

                    //Act: Get Reports
                    var reports = await technicalSupervisorController.getReports();
                    var reportsOkObjectResult = reports as OkObjectResult;
                    var reportsList = reportsOkObjectResult.Value as List<Report>;
                    var handledReport = reportsList.FirstOrDefault(r =>
                        r.reportID == 2453 &&
                        r.reportStatus == "Resolved");

                    Assert.NotNull(handledReport);
                }
                else if (handleResult is BadRequestObjectResult badAssignResult)
                {
                    Assert.Equal("Report Not found ...", badAssignResult.Value);
                }
                else
                {
                    Assert.Equal("Fail", "Pass");
                }
            }
            else if (getResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("Reports not found ...", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }

    [Fact]
    public async Task Login_CheckRequests_AssignRequest()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: Get all new requests 
            var getResult = await technicalSupervisorController.GetNewRequestByUserId(userCredentials.User_Id);

            if (getResult is OkObjectResult okObjectResult)
            {
                Assert.IsType<OkObjectResult>(getResult);

                //Act Assign request 
                var assignResult = await technicalSupervisorController.AssignRequest("2222222", 89);

                if (assignResult is OkObjectResult okAssignResult)
                {
                    Assert.Equal("Request assigned successfully", okAssignResult.Value);
                    // Act: Get requests
                    var requests = await technicalSupervisorController.getRequests();
                    var requestsOkObjectResult = requests as OkObjectResult;
                    var requestsList = requestsOkObjectResult.Value as List<Service>;
                    var assignedRequest = requestsList.FirstOrDefault(r =>
                        r.RequestID == 89 &&
                        r.AssignedTo == "2222222");

                    Assert.NotNull(assignedRequest);
                }
                else if (assignResult is BadRequestObjectResult badAssignResult)
                {
                    Assert.Equal("Request could not be assigned..", badAssignResult.Value);
                }
                else
                {
                    Assert.Equal("Fail", "Pass");
                }
            }
            else if (getResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("No requests fond...", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }

    [Fact]
    public async Task Login_CheckRequests_HandleRequest()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: Get all new requests 
            var getResult = await technicalSupervisorController.GetNewRequestByUserId(userCredentials.User_Id);

            if (getResult is OkObjectResult okObjectResult)
            {
                Assert.IsType<OkObjectResult>(getResult);

                //Act Handle Request
                var handleResult =
                    await technicalMemberrController.handelRequest(89, "Yes from integration testing  3", "approved");

                if (handleResult is OkObjectResult okAssignResult)
                {
                    Assert.Equal("Request handled successfully", okAssignResult.Value);
                    //Act: Get requests
                    var requests = await facultyMemberController.getMyRequests("1111111");
                    var requestsOkObjectResult = requests as OkObjectResult;
                    var requestsList = requestsOkObjectResult.Value as List<Service>;
                    var assignedRequest = requestsList.FirstOrDefault(r =>
                        r.RequestID == 89 &&
                        r.RequestStatus == "approved");

                    Assert.NotNull(assignedRequest);
                }
                else if (handleResult is BadRequestObjectResult badAssignResult)
                {
                    Assert.Equal("Could not handel request", badAssignResult.Value);
                }
                else
                {
                    Assert.Equal("Fail", "Pass");
                }
            }
            else if (getResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("No requests fond...", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }

    [Fact]
    public async Task Login_SearchForDevice()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: Search a Devcie
            var searchResult = await technicalMemberrController.SearchForDevice("SN-L1-1");

            if (searchResult is OkObjectResult okObjectResult)
            {
                Assert.IsType<OkObjectResult>(okObjectResult);
            }
            else if (searchResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("Device not found...", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }

    [Fact]
    public async Task Login_AddNewDevice()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: Add a new Device
            var addResult = await technicalMemberrController.AddDevice("SN-L1-1", "PC", "1");

            if (addResult is OkObjectResult okObjectResult)
            {
                Assert.Equal("Device added successfully", okObjectResult.Value);
                //Act: Search new device
                var addedDevice = await technicalMemberrController.SearchForDevice("SN-L1-1");
                Assert.IsType<OkObjectResult>(addedDevice);
            }
            else if (addResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("Device already exists!", badRequestObject.Value);
            }
            else if (addResult is ConflictObjectResult conflictObjectResult)
            {
                Assert.Equal("No enough capacity for the new device", conflictObjectResult.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }

    [Fact]
    public async Task Login_DeleteDevice()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: Delete Device
            var deleteResult = await technicalMemberrController.DeleteDeviceBySerialNumber("SN-L1-1");

            if (deleteResult is OkObjectResult okObjectResult)
            {
                Assert.Equal("Device deleted successfully!", okObjectResult.Value);
                //Act: Search the deleted device
                var deletedDevice = await technicalMemberrController.SearchForDevice("SN-L1-1");
                Assert.IsType<BadRequestObjectResult>(deletedDevice);
            }
            else if (deleteResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("Device Not found!", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }

    [Fact]
    public async Task Login_DashBoard()
    {
        //Arrange

        var userCredentials = new
        {
            User_Id = "3333333",
            Password = "1234567"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: Show DashBoard and get ReportStatistics
            var reportStatisticsResult = await technicalSupervisorController.GetReportStatistics();

            if (reportStatisticsResult is OkObjectResult)
            {
                Assert.IsType<OkObjectResult>(reportStatisticsResult);
            }
            else if (reportStatisticsResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("No Reports Found...", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }

            // Act: Show DashBoard and get TeamProgress
            var teamProgressResult = await technicalSupervisorController.GetTeamProgress();

            if (teamProgressResult is OkObjectResult)
            {
                Assert.IsType<OkObjectResult>(teamProgressResult);
            }
            else if (teamProgressResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("Could not gather data", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }


            // Act: Show DashBoard and get deviceStatisticsResult
            var deviceStatisticsResult = await technicalSupervisorController.GetDevicesStatistics();

            if (deviceStatisticsResult is OkObjectResult)
            {
                Assert.IsType<OkObjectResult>(deviceStatisticsResult);
            }
            else if (deviceStatisticsResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("No Devices Found...", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }

            // Act: get unchecked results
            var reportsResult = await technicalSupervisorController.MonitorReports();

            if (reportsResult is OkObjectResult)
            {
                Assert.IsType<OkObjectResult>(reportsResult);
            }
            else if (reportsResult is BadRequestObjectResult badRequestObject)
            {
                Assert.Equal("No Reports found ..", badRequestObject.Value);
            }

            else
            {
                Assert.Equal("Fail", "Pass");
            }
        }
        else if (loginResult is BadRequestObjectResult badAssignResult)
        {
            Assert.Equal("Invalid user ID or password.", badAssignResult.Value);
        }
        else
        {
            Assert.Equal("Fail", "Pass");
        }
    }
}