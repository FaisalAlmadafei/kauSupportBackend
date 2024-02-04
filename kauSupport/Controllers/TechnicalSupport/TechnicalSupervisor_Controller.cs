using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using Dapper;
using kauSupport.Models;
using Microsoft.AspNetCore.Mvc;

namespace kauSupport.Controllers.TechnicalSupport;
[Route("api/[controller]")]
[ApiController]
public class TechnicalSupervisor_Controller : Controller
{
    private readonly IConfiguration config;
    private SqlConnection conn;


    public TechnicalSupervisor_Controller(IConfiguration config)
    {
        this.config = config;
        conn = conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));

        
    }
    //-----------------------------------------------Get all reports----------------------------------------------------
    [HttpGet]
    [Route("GetReports")]
    public async Task<ActionResult> getReports()
    {
        var response = await conn.QueryAsync<Report>("select * from  [kauSupport].[dbo].[Reports]");
        if (response.Any())
        {
            return Ok(response);
        }
        else
        {
            return BadRequest("No reports found...");
        }
       
    }

    //----------------------------------------------Assign report to a technical member---------------------------------
    [HttpPut]
    [Route("AssignReport")]
    public async Task<ActionResult> AssignReport([Required] string User_Id, [Required] int Report_Id)
    {
        string status = "in process";
        var affectedRows = await conn.ExecuteAsync(
            "UPDATE [kauSupport].[dbo].[Reports] set assignedTaskTo= @assignedTaskTo, reportStatus= @reportStatus WHERE reportID= @reportID",
            new
            {
                assignedTaskTo = User_Id,
                reportID = Report_Id,
                reportStatus = status
            });


        if (affectedRows > 0)
        {
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Notifications] set userId= @userId WHERE reportID= @reportID",
                new
                {
                    reportID = Report_Id,
                    userId = User_Id
                });

            return Ok("Report assigned successfully ");
        }
        else
        {
            return BadRequest("Could not assign Report...");
        }
    }
    
    //--------This method will return all new report and will check the database if a device needs periodic maintenance-
    [HttpGet]
    [Route("GetNewReportsForSupervisor")]
    public async Task<ActionResult> GetNewReportsForSupervisor()
    {
        DateTime currentDateTime = DateTime.Now.Date;
        DateTime newPeriodicMaintenanceDate = currentDateTime.AddMonths(6);
        Console.WriteLine(currentDateTime);
        //First we will store all devices with perodic data is today ...
        var devices = await conn.QueryAsync<Device>(
            "select * from  [kauSupport].[dbo].[Devices] where nextPeriodicDate<= @nextPeriodicDate",
            new { nextPeriodicDate = currentDateTime });
        //we will get supervisorID
        var supervisorID = await conn.QueryFirstOrDefaultAsync<User>(
            "select UserId from  [kauSupport].[dbo].[Users] WHERE role = @role",
            new { role = "Supervisor" });

        // Now we will loop each device and create a new report
        foreach (var device in devices)
        {
            int Device_Number = device.deviceNumber;
            string Serial_Number = device.serialNumber;
            string Device_LocatedLab = device.deviceLocatedLab;
            string Report_Type = "Periodic maintenance";
            string Problem_Description = "The device needs a Periodic maintenance";
            string Reported_By = "System";
            string Assigned_To = supervisorID.UserId;

            // here we update the nextPeriodicDate
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Devices] SET nextPeriodicDate = @nextPeriodicDate WHERE deviceNumber = @deviceNumber",
                new { nextPeriodicDate = newPeriodicMaintenanceDate, deviceNumber = Device_Number });
            string status = "Reported";
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Devices] SET deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber; ",
                new { serialNumber = Serial_Number, deviceStatus = status });

            // we add a new report to reports table..
            var Report_ID = await conn.QuerySingleAsync<int>(
                "INSERT INTO  [kauSupport].[dbo].[Reports] ( deviceNumber, serialNumber,deviceLocatedLab, reportType , problemDescription, reportedBy , reportDate, assignedTaskTo) values " +
                "( @deviceNumber, @serialNumber, @deviceLocatedLab, @reportType , @problemDescription, @reportedBy ,@reportDate, @assignedTaskTo );  SELECT CAST(SCOPE_IDENTITY() as int); ",
                new
                {
                    deviceNumber = Device_Number,
                    serialNumber = Serial_Number,
                    deviceLocatedLab = Device_LocatedLab,
                    reportType = Report_Type,
                    problemDescription = Problem_Description,
                    reportedBy = Reported_By,
                    reportDate = currentDateTime,
                    assignedTaskTo = Assigned_To
                });


            await conn.ExecuteAsync(
                "INSERT INTO  [kauSupport].[dbo].[Notifications] ( reportId, userId, NotificationType) values (@reportId, @userId, @NotificationType ) ",
                new
                {
                    reportId = Report_ID,
                    userId = supervisorID.UserId, // Supervisor ID by defualt
                    NotificationType = Report_Type
                });
        }

        // we will get all reports with reportType = Periodic maintenance .....
        var response = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where  reportStatus =@reportStatus",
            new { reportStatus = "Pending" });
        if (response.Any())
        {
            return Ok(response);
        }
        else
        {
            return NotFound("Reports not found ...");
        }
    }
    //---------------------------------Get new request for a technical member by UserID---------------------------------
    [HttpGet]
    [Route("GetNewRequestByUserId")]
    public async Task<ActionResult> GetNewRequestByUserId(string User_Id)
    {
        var response = await conn.QueryAsync<Service>(
            "select * from  [kauSupport].[dbo].[Services] where AssignedTo= @AssignedTo AND RequestStatus= @RequestStatus",
            new
            {
                AssignedTo = User_Id,
                RequestStatus = "Pending"
            });
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No requests fond...");
        }
    }
    
    //-------------------------------Supervisor can see the status of reports assigned to his team----------------------
    [HttpGet]
    [Route("MonitorReports")]
    public async Task<ActionResult> MonitorReports()
    {
      
        var response = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where checkedBySupervisor= @checkedBySupervisor",
            new
            {
                checkedBySupervisor = "No"
            });
        if (response.Any())
        {
            return Ok(response);
        }
        else
        {
            return BadRequest("No Reports found ..");
        }
        
    }
    //------------------------------------Check report by supervisor------------------------------------

    [HttpPut]
    [Route("CheckReport")]
    public async Task<ActionResult> CheckReport([Required]int Report_ID)
    {
        var affectedRows = await conn.ExecuteAsync(
            "UPDATE [kauSupport].[dbo].[Reports] set checkedBySupervisor= @checkedBySupervisor where reportID= @reportID",
            new
            {
                checkedBySupervisor = "Yes",
                reportID= Report_ID
            });
        if (affectedRows > 0)
        {
            return Ok("Report checked ");
        }
        else
        {
            return BadRequest("Could not check the report.");
        }
        
    }


    //------------------------------------Assign services request to supervisor team------------------------------------
    [HttpPost]
    [Route("AssignRequest")]
    public async Task<ActionResult> AssignRequest(string User_Id, int Request_Id)
    {
        var affectedRows = await conn.ExecuteAsync(
            "UPDATE [kauSupport].[dbo].[Services] set AssignedTo= @AssignedTo WHERE RequestID= @RequestID",
            new
            {
                AssignedTo = User_Id,
                RequestID = Request_Id
            });


        if (affectedRows > 0)
        {
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Notifications] set userId= @userId WHERE reportID= @reportID AND NotificationType=@NotificationType",
                new
                {
                    reportID = Request_Id,
                    userId = User_Id,
                    NotificationType = "Service Request"
                });

            return Ok("Request assigned successfully");
        }
        else
        {
            return BadRequest("Request could not be assigned..");
        }
    }

 
}