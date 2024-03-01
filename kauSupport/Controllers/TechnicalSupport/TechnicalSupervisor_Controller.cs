using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using Dapper;
using kauSupport.Controllers.UserVerification;
using kauSupport.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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
        try
        {
            string status = "in process";

            var technicalMember = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM [kauSupport].[dbo].[Users] WHERE userId = @userId",
                new { userId = User_Id });

            if (technicalMember == null)
            {
                return BadRequest("User not found.");
            }

            var affectedRows = await conn.ExecuteAsync(
                @"UPDATE [kauSupport].[dbo].[Reports] 
              SET assignedTaskTo = @assignedTaskTo, 
                  reportStatus = @reportStatus, 
                  assignedToFirstName = @assignedToFirstName, 
                  assignedToLastName = @assignedToLastName 
              WHERE reportID = @reportID",
                new
                {
                    assignedTaskTo = User_Id,
                    reportID = Report_Id,
                    reportStatus = status,
                    assignedToFirstName = technicalMember.firstName,
                    assignedToLastName = technicalMember.lastName
                });

            if (affectedRows > 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE [kauSupport].[dbo].[Notifications] SET userId = @userId WHERE reportID = @reportID",
                    new { reportID = Report_Id, userId = User_Id });

                return Ok("Report assigned successfully.");
            }
            else
            {
                return BadRequest("Could not assign Report.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"An error occurred: {ex.Message}");
            return StatusCode(500, "Internal server error.");
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
        //we will get supervisor 
        var supervisor = await conn.QueryFirstOrDefaultAsync<User>(
            "select * from  [kauSupport].[dbo].[Users] WHERE role = @role",
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
            string Assigned_To = supervisor.UserId;
            string problem_Type = "Periodic maintenance";
            string reportedBy_FirstName = "KauSupport";
            string reportedBy_LastName = "System";
            string assignedTo_FirstName = supervisor.firstName;
            string assignedTo_LastName = supervisor.lastName;

            // here we update the nextPeriodicDate
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Devices] SET nextPeriodicDate = @nextPeriodicDate WHERE serialNumber = @serialNumber",
                new { nextPeriodicDate = newPeriodicMaintenanceDate, serialNumber = Serial_Number });
            string status = "Reported";
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Devices] SET deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber; ",
                new { serialNumber = Serial_Number, deviceStatus = status });

            // we add a new report to reports table..
            var Report_ID = await conn.QuerySingleAsync<int>(
                "INSERT INTO  [kauSupport].[dbo].[Reports] ( deviceNumber, serialNumber,deviceLocatedLab, reportType , problemDescription, reportedBy , reportDate, assignedTaskTo ,  problemType , reportedByFirstName ,reportedByLastName ,assignedToFirstName ,assignedToLastName) values " +
                "( @deviceNumber, @serialNumber, @deviceLocatedLab, @reportType , @problemDescription, @reportedBy ,@reportDate, @assignedTaskTo, @problemType,@reportedByFirstName ,@reportedByLastName ,@assignedToFirstName ,@assignedToLastName );  SELECT CAST(SCOPE_IDENTITY() as int); ",
                new
                {
                    deviceNumber = Device_Number,
                    serialNumber = Serial_Number,
                    deviceLocatedLab = Device_LocatedLab,
                    reportType = Report_Type,
                    problemDescription = Problem_Description,
                    reportedBy = Reported_By,
                    reportDate = currentDateTime,
                    assignedTaskTo = Assigned_To,
                    problemType = problem_Type,
                    reportedByFirstName = reportedBy_FirstName,
                    reportedByLastName = reportedBy_LastName,
                    assignedToFirstName = assignedTo_FirstName,
                    assignedToLastName = assignedTo_LastName
                });


            await conn.ExecuteAsync(
                "INSERT INTO  [kauSupport].[dbo].[Notifications] ( reportId, userId, NotificationType) values (@reportId, @userId, @NotificationType ) ",
                new
                {
                    reportId = Report_ID,
                    userId = supervisor.UserId, // Supervisor ID by defualt
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
            return BadRequest("Reports not found ...");
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
    public async Task<ActionResult> CheckReport([Required] int Report_ID)
    {
        var affectedRows = await conn.ExecuteAsync(
            "UPDATE [kauSupport].[dbo].[Reports] set checkedBySupervisor= @checkedBySupervisor where reportID= @reportID",
            new
            {
                checkedBySupervisor = "Yes",
                reportID = Report_ID
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
    [HttpPut]
    [Route("AssignRequest")]
    public async Task<ActionResult> AssignRequest(string User_Id, int Request_Id)
    {
        var technicalMember = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM [kauSupport].[dbo].[Users] WHERE userId = @userId",
            new { userId = User_Id });

        if (technicalMember == null)
        {
            return BadRequest("User not found.");
        }

        var affectedRows = await conn.ExecuteAsync(
            "UPDATE [kauSupport].[dbo].[Services] set AssignedTo= @AssignedTo , assignedToFirstName=@assignedToFirstName ,assignedToLastName=@assignedToLastName  WHERE RequestID= @RequestID",
            new
            {
                AssignedTo = User_Id,
                RequestID = Request_Id,
                assignedToFirstName = technicalMember.firstName,
                assignedToLastName = technicalMember.lastName
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

    //-----------------------------------------------------------------------------------------------------------------
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

    //-----------------------------------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetTeamProgress")]
    public async Task<ActionResult> GetTeamProgress()
    {
        var TeamProgress = new List<TeamMembersProgress>();

        var Team = await conn.QueryAsync<User>(
            "select UserId, firstName, lastName, role,email  from   [kauSupport].[dbo].[Users] WHERE role= @role",
            new
            {
                role = "Technical Member"
            });


        foreach (var member in Team)

        {
            var notificationTypes = new[] { "issue", "Periodic maintenance" };
            var ReportsAssignedCount = await conn.QuerySingleAsync<int>(
                "select COUNT(*) from  [kauSupport].[dbo].[Notifications] where userId= @userId And NotificationType IN @NotificationType",
                new
                {
                    userId = member.UserId,
                    NotificationType = notificationTypes
                });

            TeamProgress.Add(new TeamMembersProgress
            {
                firstName = member.firstName,
                lastName = member.lastName,
                numberOfReports = ReportsAssignedCount
            });
        }

        if (TeamProgress.Any())

        {
            return Ok(TeamProgress);
        }

        else
        {
            return BadRequest("Could not gather data");
        }
    }

    //-----------------------------------------------------------------------------------------------------------------


    [HttpGet]
    [Route("GetReportStatistics")]
    public async Task<ActionResult> GetReportStatistics()
    {
        // Query to get the total number of reports
        var reportsTotalCount = await conn.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM [kauSupport].[dbo].[Reports] ");
        if (reportsTotalCount <= 0)
        {
            return BadRequest("No Reports Found...");
        }

        // Query to get the count for each problem type of report
        var reportsWithTypeCounts = await conn.QueryAsync<ReportSummary>(
            "SELECT problemType, COUNT(*) AS Count FROM [kauSupport].[dbo].[Reports] GROUP BY problemType");


        var reportsSummaryList = new List<ReportSummary>();

        foreach (var report in reportsWithTypeCounts)
        {
            reportsSummaryList.Add(
                new ReportSummary
                {
                    problemType = report.problemType,
                    Count = report.Count,
                    Percentage =
                        ((double)report.Count / reportsTotalCount) *
                        100 // Make sure totalCount is not 0 to avoid division by zero
                }
            );
        }

        // Construct the final DTO
        var result = new TotalReportSummary
        {
            ReportsTotalCount = reportsTotalCount,
            Details = reportsSummaryList
        };

        return Ok(result);
    }
}