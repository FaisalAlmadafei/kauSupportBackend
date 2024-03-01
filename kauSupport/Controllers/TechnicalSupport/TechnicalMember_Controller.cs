using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Runtime.InteropServices.JavaScript;
using Azure.Security.KeyVault.Secrets;
using Dapper;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI_API;

namespace kauSupport.Controllers.TechnicalSupport;

[Route("api/[controller]")]
[ApiController]
public class TechnicalMember_Controller : Controller
{
    private readonly IConfiguration config;
    private SqlConnection conn;


    public TechnicalMember_Controller(IConfiguration config)
    {
        this.config = config;
        conn = conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    }

    //------------------------------------Get reports by memberID-------------------------------------------------------
    [HttpGet]
    [Route("GetReportsByTechnicalMemberID")]
    public async Task<ActionResult> GetReportsByTechnicalMemberID([Required] string User_Id)
    {
        string Report_Type = "issue";
        string Report_Status = "in process";
        var response = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where assignedTaskTo= @assignedTaskTo  AND reportStatus = @reportStatus",
            new { assignedTaskTo = User_Id, reportType = Report_Type, reportStatus = Report_Status });

        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("Reports not found ...");
        }
    }

    //----------------------------------Get the Periodic Maintenance Reports by memberID--------------------------------
  /*  [HttpGet]
    [Route("GetPeriodicMaintenanceReportsByMemberID")]
    public async Task<ActionResult> GetPeriodicMaintenanceReportsByMemberID([Required] string User_Id)
    {
        var response = await conn.QueryAsync<Report>(
            "SELECT * FROM [kauSupport].[dbo].[Reports] " +
            "WHERE reportType = @reportType AND assignedTaskTo = @assignedTaskTo AND reportStatus = @reportStatus",
            new { reportType = "Periodic maintenance", assignedTaskTo = User_Id, reportStatus = "in process" });

        if (response.Any())
        {
            return Ok(response);
        }
        else
        {
            return BadRequest("Reports not found ...");
        }
    }
*/

    //-----------------------------------------Get the device with all reports on it------------------------------------
    [HttpGet]
    [Route("SearchForDevice")]
    public async Task<ActionResult> SearchForDevice([Required] string Serial_Number)
    {
        // we get the devices
        var returnedDevice = await conn.QueryFirstOrDefaultAsync<Device>(
            "select * from  [kauSupport].[dbo].[Devices] where serialNumber = @serialNumber",
            new { serialNumber = Serial_Number });
        if (returnedDevice == null)
        {
            return BadRequest("Device not found...");
        }

        // we get all reports on that devcie
        var reportsOnDevice = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where serialNumber= @serialNumber",
            new { serialNumber = Serial_Number });
        // We create an object of what we want to return and we add the device and report list
        DeviceReports deviceAndReports = new DeviceReports();
        deviceAndReports.device = returnedDevice;
        deviceAndReports.reports = reportsOnDevice;

        return Ok(deviceAndReports);
    }

    //---------------------------------------Delete a device by serial number-------------------------------------------
    [HttpDelete]
    [Route("DeleteDeviceBySerialNumber")]
    public async Task<ActionResult> DeleteDeviceBySerialNumber([Required] string Serial_Number)
    {
        int rowsAffected = await conn.ExecuteAsync(
            "DELETE FROM [kauSupport].[dbo].[Devices] WHERE serialNumber = @serialNumber",
            new { serialNumber = Serial_Number });

        if (rowsAffected > 0)
        {
            return Ok("Device deleted successfully!");
        }
        else
        {
            // No rows affected, device not found
            return BadRequest("Device Not found!");
        }
    }

    //------------------------------------------------Add new device ---------------------------------------------------
    [HttpPost]
    [Route("AddDevice")]
    public async Task<ActionResult> AddDevice(
        [Required] string Serial_Number,
        [Required] string Device_Type,
        [Required] string Device_LocatedLab
    )

    {
        DateTime Arrival_Date = DateTime.Now.Date;
        DateTime Next_Periodic_Date = Arrival_Date.AddMonths(6);

        // Check if the device already exists
        var deviceExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM [kauSupport].[dbo].[Devices] WHERE serialNumber = @serialNumber",
            new { serialNumber = Serial_Number });
        if (deviceExists > 0)
        {
            // Device already exists, return a conflict response
            return BadRequest("Device already exists!");
        }

        // Get the capacity of the lab before adding a new device to it
        int labCapacity = await conn.ExecuteScalarAsync<int>(
            "SELECT labCapacity FROM [kauSupport].[dbo].[Labs] WHERE labNumber = @labNumber",
            new { labNumber = Device_LocatedLab });

        // Get the list of existing device numbers in the same lab
        var existingDeviceNumbers = await conn.QueryAsync<int>(
            "SELECT deviceNumber FROM [kauSupport].[dbo].[Devices] WHERE deviceLocatedLab = @deviceLocatedLab",
            new { deviceLocatedLab = Device_LocatedLab });

        int newDeviceNumber = 1; // Default device number

        // Check for the first available device number in a sequential manner
        while (existingDeviceNumbers.Contains(newDeviceNumber))
        {
            newDeviceNumber++;
        }

        if (newDeviceNumber <= labCapacity)
        {
            await conn.ExecuteAsync(
                "INSERT INTO [kauSupport].[dbo].[Devices] (serialNumber, type, deviceLocatedLab, arrivalDate, nextPeriodicDate, deviceNumber) VALUES(@serialNumber, @type, @deviceLocatedLab, @arrivalDate, @nextPeriodicDate, @deviceNumber)",
                new
                {
                    serialNumber = Serial_Number,
                    type = Device_Type,
                    deviceLocatedLab = Device_LocatedLab,
                    arrivalDate = Arrival_Date,
                    nextPeriodicDate = Next_Periodic_Date,
                    deviceNumber = newDeviceNumber
                });

            return Ok("Device added successfully");
        }
        else
        {
            return Conflict("No enough capacity for the new device");
        }
    }

    //----------------------------------Update devcie-------------------------------------------------------------------
    [HttpPut]
    [Route("UpdateDevice")]
    public async Task<ActionResult> UpdateDevice(string Serial_Number, string Device_Status, string Device_Type,
        DateTime NextPeriodic_Date, DateTime Arrival_Date, int Device_Number, string Lab_Number)
    {
        var effectedRows =
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Devices] SET serialNumber = @serialNumber, deviceStatus = @deviceStatus," +
                " type = @type, arrivalDate = @arrivalDate, nextPeriodicDate = @nextPeriodicDate WHERE deviceNumber = @deviceNumber AND " +
                "deviceLocatedLab = @deviceLocatedLab",
                new
                {
                    serialNumber = Serial_Number,
                    deviceStatus = Device_Status,
                    type = Device_Type,
                    arrivalDate = Arrival_Date,
                    nextPeriodicDate = NextPeriodic_Date,
                    deviceNumber = Device_Number,
                    deviceLocatedLab = Lab_Number
                });

        if (effectedRows > 0)
        {
            return Ok("Device updated successfully!");
        }
        else
        {
            return BadRequest("Problem in updating the device");
        }
    }

    // ----------------------------------------Handel report and fix problem-------------------------------------------
    [HttpPut]
    [Route("handelReport")]
    public async Task<ActionResult> handelReport([Required] int Report_Id, [Required] string Action_Taken)
    {
        DateTime Repair_Date = DateTime.Now.Date;
        string Report_Status = "Resolved";

        var report = await conn.QueryFirstOrDefaultAsync<Report>(
            "SELECT reportType from [kauSupport].[dbo].[Reports] where reportID = @reportID ",
            new { reportID = Report_Id });

        if (report == null)
        {
            return BadRequest("Report Not found ...");
        }

        //Update report data
        await conn.ExecuteAsync(
            "UPDATE  [kauSupport].[dbo].[Reports] set reportStatus = @reportStatus, actionTaken= @actionTaken ,repairDate = @repairDate where reportID = @reportID",
            new
            {
                reportStatus = Report_Status,
                actionTaken = Action_Taken,
                repairDate = Repair_Date,
                reportID = Report_Id
            });
        // Will get the serial number of the device
        var deviceSerialNumber = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT serialNumber from [kauSupport].[dbo].[Reports] where reportID = @reportID ",
            new { reportID = Report_Id });

        string newStatus = "Working";
        await conn.ExecuteAsync(
            "UPDATE  [kauSupport].[dbo].[Devices] set deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber",
            new
            {
                deviceStatus = newStatus,
                serialNumber = deviceSerialNumber
            });

        //Remove the cookies
        HttpContext.Response.Cookies.Delete("ReportedDevice_" + deviceSerialNumber);
        //Delete the notification
        await conn.ExecuteAsync(
            "Delete from [kauSupport].[dbo].[Notifications]  WHERE reportID= @reportID AND NotificationType= @NotificationType",
            new
            {
                reportID = Report_Id,
                NotificationType = report.reportType
            });

        return Ok("Report handled successfully!");
    }


    // ------------------------Method that uses chat GBT API to suggest a solution for a problem------------------------
    [HttpGet]
    [Route("SuggestSolution")]
    public async Task<IActionResult> SuggestSolution(string problem)
    {
        var myKey = await conn.QueryFirstOrDefaultAsync<string>(
            "select mykey from  [kauSupport].[dbo].[API]");


        var openAi = new OpenAIAPI(new APIAuthentication(myKey));

        var conv = openAi.Chat.CreateConversation();

        //
        conv.AppendSystemMessage(
            "You are the technical support assistant. Provide direct and clear solutions for PC problems reported by users. Avoid suggesting to contact technical support or seek external help. Focus on actionable steps that can be taken immediately.");

        // Sample user inputs with expected chatbot outputs
        conv.AppendUserInput("Slow computer performance");
        conv.AppendExampleChatbotOutput("1-Run a disk cleanup and defragmentation./n" +
                                        "2- Disable unnecessary startup programs./n");

        conv.AppendUserInput("Printer not responding");
        conv.AppendExampleChatbotOutput("1- Ensure printer is turned on and connected./n " +
                                        "2- Check for printer driver updates./n");
        conv.AppendUserInput("الطابعة ما تشتغل");
        conv.AppendExampleChatbotOutput("1- Ensure printer is turned on and connected./n " +
                                        "2- Check for printer driver updates./n");

        conv.AppendUserInput("My computer won't boot or light up");
        conv.AppendExampleChatbotOutput("Look for signs of power, and ensure the monitor is turned on and connected.");


        // Handling the actual user input
        conv.AppendUserInput(problem + "Result in English");
        var response = await conv.GetResponseFromChatbotAsync();

        return Ok(response);
    }

    //-------------------------------------------Get all notifications--------------------------------------------------
    [HttpGet]
    [Route("GetNotifications")]
    public async Task<ActionResult> getNotifications()
    {
        var response = await conn.QueryAsync<Notification>("select * from  [kauSupport].[dbo].[Notifications]");
        if (response.Any())
        {
            return Ok(response);
        }

        else
        {
            return BadRequest("No Notifications found found...");
        }
    }

    //----------------------------Get report Notifications for a technical support member-------------------------------
    [HttpGet]
    [Route("GetReportsNotificationsByUserId")]
    public async Task<ActionResult> getReportsNotificationsByUserId(string User_Id)
    {
        var notificationTypes = new[] { "issue", "Periodic maintenance" }; 
        var response = await conn.QuerySingleAsync<int>(
            "select COUNT(*) from  [kauSupport].[dbo].[Notifications] where userId= @userId And NotificationType IN @NotificationType",
            new
            {
                userId = User_Id,
                NotificationType = notificationTypes
            });


        return Ok(response);
    }

    //--------------------------Get periodic maintenance Notifications for a technical support member-------------------
   /* [HttpGet]
    [Route("GetPeriodicNotificationsByUserId")]
    public async Task<ActionResult> getPeriodicNotificationsByUserId(string User_Id)
    {
        var Notification_Type = "Periodic maintenance"; // To retrieve only Periodic maintenance Notifications...
        var response = await conn.QuerySingleAsync<int>(
            "select COUNT(*) from  [kauSupport].[dbo].[Notifications] where userId= @userId And NotificationType=@NotificationType",
            new
            {
                userId = User_Id,
                NotificationType = Notification_Type
            });


        return Ok(response);
    }*/

    //--------------------------Get service requests Notifications for a technical support member-----------------------
    [HttpGet]
    [Route("GetRequestsNotificationsByUserId")]
    public async Task<ActionResult> GetRequestsNotificationsByUserId(string User_Id)
    {
        var Notification_Type = "Service Request"; // To retrieve only service requests Notifications...
        var response = await conn.QuerySingleAsync<int>(
            "select COUNT(*) from  [kauSupport].[dbo].[Notifications] where userId= @userId And NotificationType=@NotificationType",
            new
            {
                userId = User_Id,
                NotificationType = Notification_Type
            });


        return Ok(response);
    }

    //-----------------------------------------Handle requests----------------------------------------------------------
    [HttpPut]
    [Route("handelRequest")]
    public async Task<ActionResult> handelRequest([Required] int Request_Id, [Required] string Replay,
        [Required] string Status)
    {
        var affectedRowa = await conn.ExecuteAsync(
            "UPDATE  [kauSupport].[dbo].[services] set TechnicalSupportReply = @TechnicalSupportReply, RequestStatus= @RequestStatus  where RequestID = @RequestID",
            new
            {
                TechnicalSupportReply = Replay,
                RequestStatus = Status,
                RequestID = Request_Id
            });

        if (affectedRowa == 1)
        {
            await conn.ExecuteAsync(
                "Delete from [kauSupport].[dbo].[Notifications]  WHERE reportID= @reportID AND NotificationType= @NotificationType",
                new { reportID = Request_Id, NotificationType = "Service Request" });
            return Ok("Request handled successfully");
        }
        else
        {
            return BadRequest("Could not handel request");
        }
    }
}