using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using kauSupport.Connection;
using kauSupport.Models;

namespace kauSupport.Controllers.FacultyMember;

[Route("api/[controller]")]
[ApiController]
public class FacultyMember_Controller : ControllerBase
{
   
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public FacultyMember_Controller(  IDbConnectionFactory dbConnectionFactory)
    {
      
        _dbConnectionFactory = dbConnectionFactory; // Instance of SqlConnectionFactory came form dependency injection 
    }
    //------------------------------------------------------------------------------------------------------------------

    //-----------------------------------------------------Get all Labs-------------------------------------------------
    [HttpGet]
    [Route("GetLabs")]
    public async Task<ActionResult> getLabs()
    {
        
        var conn = _dbConnectionFactory.CreateConnection();
        var response = await conn.QueryAsync<Lab>("select * from  [kauSupport].[dbo].[Labs]");
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No Labs found...");
        }
    }
    //------------------------------------------------------------------------------------------------------------------
    //--------------------------------Get all Devices in a specific Lab-------------------------------------------------
    [HttpGet]
    [Route("GetLabDevices/ID=" + "{lab_Number}")]
    public async Task<ActionResult> getLabDevices(string lab_Number)
    {
        var conn = _dbConnectionFactory.CreateConnection();

        var response = await conn.QueryAsync<Device>(
            "select * from  [kauSupport].[dbo].[Devices] where deviceLocatedLab = @deviceLocatedLab ORDER BY deviceNumber",
            new { deviceLocatedLab = lab_Number });
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No devices found in this Lab...");
        }
    }
    //------------------------------------------------------------------------------------------------------------------

    //--------------------------------Add new report on a device--------------------------------------------------------
    [HttpPost]
    [Route("AddReport")]
    public async Task<ActionResult> addReport([Required] String Device_Number, [Required] String Serial_Number,
        [Required] String Device_LocatedLab,
        [Required] String Problem_Description, [Required] String Reported_By, [Required] String Problem_Type)
    {
        var conn = _dbConnectionFactory.CreateConnection();

        DateTime currentDateTime = DateTime.Now.Date;
        string Report_Type = "issue";

    
        // we will get the Admin or supervisor to use their ID
        var Supervisor = await conn.QueryFirstOrDefaultAsync<User>(
            "select * from  [kauSupport].[dbo].[Users] WHERE role = @role",
            new { role = "Supervisor" });
        // we will get the user first and last name
        var FacultyMember = await conn.QueryFirstOrDefaultAsync<User>(
            "select * from  [kauSupport].[dbo].[Users] WHERE userId = @userId",
            new { userId = Reported_By });


        // we will add new report and get the report ID
        var Report_ID = await conn.QuerySingleAsync<int>(
            "INSERT INTO  [kauSupport].[dbo].[Reports] ( deviceNumber, serialNumber,deviceLocatedLab, reportType , problemDescription, reportedBy , reportDate, assignedTaskTo , problemType ,   assignedToFirstName , assignedToLastName ,  reportedByFirstName  , reportedByLastName ) values" +
            " ( @deviceNumber, @serialNumber, @deviceLocatedLab, @reportType , @problemDescription, @reportedBy ,@reportDate, @assignedTaskTo , @problemType ,  @assignedToFirstName , @assignedToLastName ,  @reportedByFirstName  , @reportedByLastName);  SELECT CAST(SCOPE_IDENTITY() as int); ",
            new
            {
                deviceNumber = Device_Number, serialNumber = Serial_Number, deviceLocatedLab = Device_LocatedLab,
                reportType = Report_Type, problemDescription = Problem_Description, reportedBy = Reported_By,
                reportDate = currentDateTime, assignedTaskTo = Supervisor.UserId, problemType = Problem_Type,
                assignedToFirstName = Supervisor.firstName, assignedToLastName = Supervisor.lastName,
                reportedByFirstName = FacultyMember.firstName, reportedByLastName = FacultyMember.lastName
            });

        string status = "Reported";
        await conn.ExecuteAsync(
            "UPDATE [kauSupport].[dbo].[Devices] SET deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber; ",
            new { serialNumber = Serial_Number, deviceStatus = status });

        // We weill add new notification 
        await conn.ExecuteAsync(
            "INSERT INTO  [kauSupport].[dbo].[Notifications] ( reportId, userId, NotificationType) values (@reportId, @userId, @NotificationType ) ",
            new
            {
                reportId = Report_ID,
                userId = Supervisor.UserId,
                NotificationType = "issue"
            });

        return Ok("Report added successfully");
    }
    //------------------------------------------------------------------------------------------------------------------
    //-------------------------------Get user previous reports by userID------------------------------------------------
    [HttpGet]
    [Route("GetMyReports")]
    public async Task<ActionResult> getMyReports([Required] string User_Id)
    {
        var conn = _dbConnectionFactory.CreateConnection();

        var response = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where reportedBy= @reportedBy ORDER BY reportID DESC",
            new { reportedBy = User_Id });
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("You have not reported any device yet...");
        }
    }
    //------------------------------------------------------------------------------------------------------------------

    //-------------------------------Get Lab with devices working and reported counts, for monitoring-------------------
    [HttpGet]
    [Route("GetLabsWithDeviceCounts")]
    public async Task<ActionResult> GetLabsWithDeviceCounts()
    {
        // Retrieve all labs
        var conn = _dbConnectionFactory.CreateConnection();

        var labs = await conn.QueryAsync<Lab>("SELECT * FROM [kauSupport].[dbo].[Labs]");
        var labsWithDeviceCountsList = new List<LabWithDeviceCounts>();

        if (!labs.Any())
        {
            return BadRequest("No Labs found");
        }

        // Now we will lopp over each device
        foreach (var lab in labs)
        {
            // Get reports counts
            var reportedCount = await conn.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM [kauSupport].[dbo].[Devices] WHERE deviceLocatedLab = @deviceLocatedLab AND deviceStatus = 'Reported'",
                new { deviceLocatedLab = lab.labNumber });

            //Get working devices counts
            var workingCount = await conn.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM [kauSupport].[dbo].[Devices] WHERE deviceLocatedLab = @deviceLocatedLab AND deviceStatus = 'Working'",
                new { deviceLocatedLab = lab.labNumber });

            //Add this to the list
            labsWithDeviceCountsList.Add(
                new LabWithDeviceCounts
                {
                    LabNumber = lab.labNumber,
                    ReportedDevicesCount = reportedCount,
                    WorkingDevicesCount = workingCount,
                    Capacity = lab.labCapacity,
                    TotalDevices = reportedCount + workingCount
                }
            );
        }

        return Ok(labsWithDeviceCountsList);
    }
    //------------------------------------------------------------------------------------------------------------------

    //-------------------------------Method for requesting a service such as, unblocking react.js-----------------------
    [HttpPost]
    [Route("RequestService")]
    public async Task<ActionResult> RequestService([Required] string Request_, [Required] string Requested_By)
    {
                var conn = _dbConnectionFactory.CreateConnection();
                var supervisor = await conn.QueryFirstOrDefaultAsync<User>(
            "select * from  [kauSupport].[dbo].[Users] WHERE role = @role",
            new { role = "Supervisor" });

        var FacultyMember = await conn.QueryFirstOrDefaultAsync<User>(
            "select * from  [kauSupport].[dbo].[Users] WHERE userId = @userId",
            new { userId = Requested_By });


        var Request_Id = await conn.QuerySingleAsync<int>(
            "INSERT INTO [kauSupport].[dbo].[services] (Request, RequestedBy, AssignedTo ,assignedToFirstName , assignedToLastName ,  requestedByFirstName  , requestedByLastName) VALUES (@Request , @RequestedBy, @AssignedTo, @assignedToFirstName , @assignedToLastName ,  @requestedByFirstName  , @requestedByLastName); SELECT CAST(SCOPE_IDENTITY() as int)",
            new
            {
                Request = Request_,
                RequestedBy = Requested_By,
                AssignedTo = supervisor.UserId,
                assignedToFirstName = supervisor.firstName,
                assignedToLastName = supervisor.lastName,
                requestedByFirstName = FacultyMember.firstName,
                requestedByLastName = FacultyMember.lastName
            });

        if (Request_Id > 0)
        {
            await conn.ExecuteAsync(
                "INSERT INTO  [kauSupport].[dbo].[Notifications] ( reportId, userId, NotificationType) values (@reportId, @userId, @NotificationType ) ",
                new
                {
                    reportId = Request_Id,
                    userId = supervisor.UserId,
                    NotificationType = "Service Request"
                });

            return Ok("Request added successfully");
        }
        else
        {
            return BadRequest("Could not add request");
        }
    }
    //------------------------------------------------------------------------------------------------------------------

    //---------------------------------------Get user requests with userID----------------------------------------------
    [HttpGet]
    [Route("GetMyRequests")]
    public async Task<ActionResult> getMyRequests([Required] string User_Id)
    {
        var conn = _dbConnectionFactory.CreateConnection();

        var response = await conn.QueryAsync<Service>(
            "select * from  [kauSupport].[dbo].[Services] where RequestedBy= @RequestedBy  ORDER BY RequestID DESC",
            new { RequestedBy = User_Id });
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No have not report any device yet...");
        }
    }
    //------------------------------------------------------------------------------------------------------------------
}