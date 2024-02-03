using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using kauSupport.Models;

namespace kauSupport.Controllers.FacultyMember;

[Route("api/[controller]")]
[ApiController]
public class FacultyMember_Controller : ControllerBase
{
    private readonly IConfiguration config;
    private SqlConnection conn;

    public FacultyMember_Controller(IConfiguration config)
    {
        this.config = config;
        conn = conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    }

    //-----------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetLabs")]
    public async Task<ActionResult> getLabs()
    {
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
    //-----------------------------------------------------------------------------------------

    [HttpGet]
    [Route("GetDevices")]
    public async Task<ActionResult> getDevices()
    {
        var response =
            await conn.QueryAsync<Device>("select * from  [kauSupport].[dbo].[Devices] ORDER BY deviceNumber");
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No devices found...");
        }
    }
    //-----------------------------------------------------------------------------------------

    [HttpGet]
    [Route("GetLabDevices/ID=" + "{lab_Number}")]
    public async Task<ActionResult> getLabDevices(string lab_Number)
    {
        var response = await conn.QueryAsync<Device>(
            "select * from  [kauSupport].[dbo].[Devices] where deviceLocatedLab = @deviceLocatedLab ORDER BY deviceNumber",
            new { deviceLocatedLab = lab_Number });
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No devices found...");
        }
    }
    //-----------------------------------------------------------------------------------------

    [HttpPost]
    [Route("AddReport")]
    public async Task<ActionResult> addReport([Required] String Device_Number, [Required] String Serial_Number,
        [Required] String Device_LocatedLab,
        [Required] String Problem_Description, [Required] String Reported_By)
    {
        DateTime currentDateTime = DateTime.Now.Date;
        string Report_Type = "issue";
        if (!IsDeviceReportable(Serial_Number))
        {
            return Conflict("This device is reported, try again within 3 days.");
        }
        // we will get adminId
        var adminID = await conn.QueryFirstOrDefaultAsync<User>("select UserId from  [kauSupport].[dbo].[Users] WHERE role = @role" , 
            new {role = "Supervisor"});

       var Report_ID= await conn.QuerySingleAsync<int>(
            "INSERT INTO  [kauSupport].[dbo].[Reports] ( deviceNumber, serialNumber,deviceLocatedLab, reportType , problemDescription, reportedBy , reportDate, assignedTaskTo) values" +
            " ( @deviceNumber, @serialNumber, @deviceLocatedLab, @reportType , @problemDescription, @reportedBy ,@reportDate, @assignedTaskTo );  SELECT CAST(SCOPE_IDENTITY() as int); ",
            new
            {
                deviceNumber = Device_Number, serialNumber = Serial_Number, deviceLocatedLab = Device_LocatedLab,
                reportType = Report_Type, problemDescription = Problem_Description, reportedBy = Reported_By,
                reportDate = currentDateTime, assignedTaskTo = adminID.UserId
            });
        string status = "Reported";
        await conn.ExecuteAsync(
            "UPDATE [kauSupport].[dbo].[Devices] SET deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber; ",
            new { serialNumber = Serial_Number, deviceStatus = status });

        HttpContext.Response.Cookies.Append(
            "ReportedDevice_" + Serial_Number,
            currentDateTime.ToString(),
            new CookieOptions
            {
                Expires = DateTime.Now.AddDays(3) // Set the cookie to expire in 3 days
            });
        
        
        await conn.ExecuteAsync(
            "INSERT INTO  [kauSupport].[dbo].[Notifications] ( reportId, userId, NotificationType) values (@reportId, @userId, @NotificationType ) ",
            new
            {
                reportId = Report_ID, 
                userId = adminID.UserId, // Supervisor ID by defualt
                NotificationType = "issue"
            });
        
        return Ok("Report added successfully");
    }

    // -----------------------------------------------------------------------------------------
    private bool IsDeviceReportable(string serialNumber)
    {
        // Check if the cookie exists and if the device has been reported within the last 3 days
        var cookieValue = HttpContext.Request.Cookies["ReportedDevice_" + serialNumber];

        if (!string.IsNullOrEmpty(cookieValue) && DateTime.TryParse(cookieValue, out var lastReportDate))
        {
            return (DateTime.Now - lastReportDate).Days >= 3;
        }

        return true; // The device can be reported if there's no cookie or if the last report is older than 3 days
    }

    //-----------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetMyReports")]
    public async Task<ActionResult> getMyReports([Required] string User_Id)
    {
        var response = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where reportedBy= @reportedBy", new { reportedBy = User_Id });
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No have not report any device yet...");
        }
    }

    //-----------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetLabsWithDeviceCounts")]
    public async Task<ActionResult> GetLabsWithDeviceCounts()
    {
        // Retrieve all labs
        var labs = await conn.QueryAsync<Lab>("SELECT * FROM [kauSupport].[dbo].[Labs]");
        var labsWithDeviceCountsList = new List<LabWithDeviceCounts>();

        foreach (var lab in labs)
        {
            // Use QuerySingleAsync to get a single integer result
            var reportedCount = await conn.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM [kauSupport].[dbo].[Devices] WHERE deviceLocatedLab = @deviceLocatedLab AND deviceStatus = 'Reported'",
                new { deviceLocatedLab = lab.labNumber });


            var workingCount = await conn.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM [kauSupport].[dbo].[Devices] WHERE deviceLocatedLab = @deviceLocatedLab AND deviceStatus = 'Working'",
                new { deviceLocatedLab = lab.labNumber });
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
    //-----------------------------------------------------------------------------------------------------------------
    [HttpPost]
    [Route("RequestService")]
    public async Task<ActionResult> RequestService([Required] string Request_, [Required] string Requested_By)
    {
        var supervisorID = await conn.QueryFirstOrDefaultAsync<User>("select UserId from  [kauSupport].[dbo].[Users] WHERE role = @role" , 
            new {role = "Supervisor"});

       var Request_Id= await conn.QuerySingleAsync<int>
            ("INSERT INTO [kauSupport].[dbo].[services] (Request, RequestedBy, AssignedTo) VALUES (@Request , @RequestedBy, @AssignedTo); SELECT CAST(SCOPE_IDENTITY() as int)",
            new
            {
                Request = Request_,
                RequestedBy = Requested_By,
                AssignedTo=supervisorID.UserId

            });

       if (Request_Id > 0)
       {
           await conn.ExecuteAsync(
               "INSERT INTO  [kauSupport].[dbo].[Notifications] ( reportId, userId, NotificationType) values (@reportId, @userId, @NotificationType ) ",
               new
               {
                   reportId = Request_Id, 
                   userId = supervisorID.UserId, // Supervisor ID by defualt
                   NotificationType = "Service Request"
               });
           
           return Ok(("Request added successfully"));
           
       }
       else
       {
           return BadRequest("Could not add request"); 
       }

    }
    //-----------------------------------------------------------------------------------------------------------------

    [HttpGet]
    [Route("GetMyRequests")]
    public async Task<ActionResult> getMyRequests([Required] string User_Id)
    {
        var response = await conn.QueryAsync<Service>(
            "select * from  [kauSupport].[dbo].[Services] where RequestedBy= @RequestedBy", new { RequestedBy = User_Id });
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No have not report any device yet...");
        }
    }


}