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

public class FacultyMemberIntegrationTests


{
    private readonly IConfiguration configuration;
    private readonly FacultyMember_Controller facultyMemberController;
    private readonly UserVerification_Controller userVerificationController;
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public FacultyMemberIntegrationTests()
    {
        this.configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        _dbConnectionFactory = new SqlConnectionFactory(configuration);
        facultyMemberController = new FacultyMember_Controller(_dbConnectionFactory);
        userVerificationController = new UserVerification_Controller(_dbConnectionFactory);
    }

    [Fact]
    public async Task Login_AddReport_CheckPreviousReports()
    {
        //Arrange


        var userCredentials = new
        {
            User_Id = "1111111",
            Password = "1234567" // Assuming the password is correct
        };
        // Sample report data
        var reportData = new
        {
            Problem_Description = "integration testing 6",
            Reported_By = "1111111", // Assuming a valid user ID
            Problem_Type = "Hardware"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);
            //Act: Get Labs
            var getLabsResult = await facultyMemberController.getLabs();
            var okResult = Assert.IsType<OkObjectResult>(getLabsResult);
            var returnedLabs = Assert.IsType<List<Lab>>(okResult.Value);
            //Act: Get Lab Devices
            var getLabDevicesResult = await facultyMemberController.getLabDevices(returnedLabs[0].labNumber);
            okResult = Assert.IsType<OkObjectResult>(getLabDevicesResult);
            var returnedLabDevices = Assert.IsType<List<Device>>(okResult.Value);

            // Act: Add a report
            var addReportResult = await facultyMemberController.addReport(
                returnedLabDevices[0].deviceNumber.ToString(),
                returnedLabDevices[0].serialNumber,
                returnedLabDevices[0].deviceLocatedLab,
                reportData.Problem_Description,
                reportData.Reported_By,
                reportData.Problem_Type);
            Assert.IsType<OkObjectResult>(addReportResult);

            // Act: Get previous reports
            var getResult = await facultyMemberController.getMyReports(userCredentials.User_Id);
            var okObjectResult = getResult as OkObjectResult;
            var reportsList = okObjectResult.Value as List<Report>;

            var previousReport = reportsList.FirstOrDefault(r =>
                r.deviceNumber == returnedLabDevices[0].deviceNumber.ToString() &&
                r.serialNumber == returnedLabDevices[0].serialNumber &&
                r.deviceLocatedLab == returnedLabDevices[0].deviceLocatedLab &&
                r.problemDescription == reportData.Problem_Description &&
                r.reportedBy == reportData.Reported_By &&
                r.problemType == reportData.Problem_Type);

            Assert.NotNull(previousReport);
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
    public async Task Login_AddRequest_CheckMyRequests()
    {
        // Arrange


        var userCredentials = new
        {
            User_Id = "1111111",
            Password = "1234567" // Assuming the password is correct
        };
        // Sample report data
        var requestData = new
        {
            Request = "Request from IntegrationTest 7",
            Requested_By = "1111111" ,
            Service_Type= "Unblock a website"
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);

            // Act: Add a request
            var addRequestResult =
                await facultyMemberController.RequestService(requestData.Request, userCredentials.User_Id ,requestData.Service_Type);
            Assert.IsType<OkObjectResult>(addRequestResult);

            // Act: Get my requests
            var getResult = await facultyMemberController.getMyRequests(userCredentials.User_Id);
            var okObjectResult = getResult as OkObjectResult;
            var requestsList = Assert.IsType<List<Service>>(okObjectResult.Value);

            var myRequest = requestsList.FirstOrDefault(r =>
                r.Request == requestData.Request &&
                r.RequestedBy == userCredentials.User_Id);

            Assert.NotNull(myRequest);
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
    public async Task Login_CheckDevicesAvailability()
    {
        // Arrange


        var userCredentials = new
        {
            User_Id = "1111111",
            Password = "1234567" // Assuming the password is correct
        };


        //Act:Login 

        var loginResult = await userVerificationController.LogIn(
            userCredentials.User_Id,
            userCredentials.Password);
        if (loginResult is OkObjectResult)
        {
            Assert.IsType<OkObjectResult>(loginResult);


            // Act: check availability
            var availabilityResult = await facultyMemberController.GetLabsWithDeviceCounts();

            if (availabilityResult is OkObjectResult okAssignResult)
            {
                Assert.IsType<OkObjectResult>(okAssignResult);
            }
            else if (availabilityResult is BadRequestObjectResult badAssignResult)
            {
                Assert.Equal("No Labs found", badAssignResult.Value);
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
