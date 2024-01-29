using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Runtime.InteropServices.JavaScript;
using Dapper;
using kauSupport.Controllers.FacultyMember;
using kauSupport.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace kauSupport.Controllers.TechnicalSupport;
[Route("api/[controller]")]
[ApiController]

public class TechnicalMember_Controller : Controller
{
    private readonly IConfiguration config;

    public TechnicalMember_Controller(IConfiguration config)
    {
        this.config = config; 
    }
    
    //------------------------------------Will get user Reports  "type = issue"-----------------------------------------------------

    [HttpGet]
    [Route("GetReportsByTechnicalMemberID")]

    public async Task<ActionResult> GetReportsByTechnicalMemberID([Required] string User_Id)
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        string Report_Type = "issue";
        string Report_Status = "in process"; 
        var response = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where assignedTaskTo= @assignedTaskTo AND reportType = @reportType AND reportStatus = @reportStatus" ,
            new { assignedTaskTo = User_Id ,reportType = Report_Type  , reportStatus = Report_Status});

        if (response.Any())
        {
            return Ok(response); 
            
        }

        else
        {
            return NotFound("Reports not found ...");
        }
    }
    //-------------------------------------will check devices that  need periodic maintenance, then add them to reports table, then show it---------------------------------------------------
    // This one better for Supervisor
    
 
    //----------------------------------get the perodic maintenance reports by memberID---------------------------------------------------
        
    [HttpGet]
    [Route("GetPeriodicMaintenanceReportsByMemberID")]
    public async Task<ActionResult> GetPeriodicMaintenanceReportsByMemberID([Required] string User_Id)
    {
        using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));

        // Use the same parameter names in the SQL query as in the anonymous object
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
            return NotFound("Reports not found...");
        }
    }

    
    
    //------------------------------------------------------------------------------------------------------------------
    [HttpGet]
    [Route("SearchForDevice")]

    public async Task<ActionResult> SearchForDevice([Required] string Serial_Number)
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
       
        // we get the devices
        var returnedDevice = await conn.QueryFirstAsync<Device>("select * from  [kauSupport].[dbo].[Devices] where serialNumber = @serialNumber" ,
            new{serialNumber = Serial_Number});
        // we get all reports on that devcie
        var reportsOnDevice = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where serialNumber= @serialNumber" ,
            new { serialNumber = Serial_Number });
        // We create an object of what we want to return and we add the devcie and report list
        DeviceReports deviceAndReports = new DeviceReports(); 
        deviceAndReports.device = returnedDevice;
        deviceAndReports.reports = reportsOnDevice;

        return Ok(deviceAndReports); 


    
    }
    //------------------------------------------------------------------------------------------------------------------

    
    [HttpDelete]
    [Route("DeleteDeviceBySerialNumber")]
    public async Task<ActionResult> DeleteDeviceBySerialNumber([Required]string Serial_Number)
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));

      
            int rowsAffected = await conn.ExecuteAsync(
                "DELETE FROM [kauSupport].[dbo].[Devices] WHERE serialNumber = @serialNumber",
                new { serialNumber = Serial_Number });

            if (rowsAffected > 0)
            {
                
                return Ok(true);
            }
            else
            {
                // No rows affected, device not found
                return Ok(false);
            }
        
      
    }
    
    //------------------------------------------------------------------------------------------------------------------

    
    [HttpPost]
    [Route("AddDevice")]
    public async Task<ActionResult> AddDevice(
        [Required] string Serial_Number,
        [Required] string Device_Type,
        [Required] string Device_LocatedLab
    )
    {
        using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
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
        // We will get the capacity of Lab before adding new device to it
        int labCapacity = await conn.ExecuteScalarAsync<int>(
            "SELECT labCapacity FROM [kauSupport].[dbo].[Labs] WHERE labNumber = @labNumber",
            new { labNumber = Device_LocatedLab });
        // We weill get number of devices in the Lab
        int numOfDevices= await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM [kauSupport].[dbo].[Devices] WHERE deviceLocatedLab = @deviceLocatedLab",
            new { deviceLocatedLab = Device_LocatedLab });

        if (numOfDevices<labCapacity)
        {
            await conn.ExecuteAsync(
                "INSERT INTO [kauSupport].[dbo].[Devices] (serialNumber, type, deviceLocatedLab, arrivalDate, nextPeriodicDate) VALUES(@serialNumber, @type, @deviceLocatedLab, @arrivalDate, @nextPeriodicDate)",
                new
                {
                    serialNumber = Serial_Number,
                    type = Device_Type,
                    deviceLocatedLab = Device_LocatedLab,
                    arrivalDate = Arrival_Date,
                    nextPeriodicDate = Next_Periodic_Date
                });


            return Ok("Device added successfully ");

        }
        else
        {
            return BadRequest("No enough capacity for the new device"); 
        }

        // Device not found, proceed with insertion
     ;
    }
    
    //------------------------------------------------------------------------------------------------------
    //TODO Update device method ... 








   // ------------------------------------------------------------------------------------------------------

          
   [HttpPost]
   [Route("handelReport")]

   public async Task<ActionResult> handelReport([Required] int Report_Id ,[Required] string Action_Taken )
   {
       var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
       DateTime Repair_Date =DateTime.Now.Date;
       string Report_Status = "Resolved";
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
       var deviceSerialNumber = await conn.QueryFirstAsync<string>(
           "SELECT serialNumber from [kauSupport].[dbo].[Reports] where reportID = @reportID ",
           new { reportID = Report_Id });
       
       string newStatus = "Working";
       await conn.ExecuteAsync(
           "UPDATE  [kauSupport].[dbo].[Devices] set deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber",
           new
           {
               deviceStatus = newStatus ,
               serialNumber = deviceSerialNumber

           });
       
       return Ok(); 



   }




   // ------------------------------------------------------------------------------------------------------
}