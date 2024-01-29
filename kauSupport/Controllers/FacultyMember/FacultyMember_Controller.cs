using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Dapper ;
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
            return Ok("No devices found .. "); 
        }
       
    }
    //-----------------------------------------------------------------------------------------
    
    [HttpGet]
    [Route("GetDevices")]
    public async Task<ActionResult> getDevices()
    {
        var response = await conn.QueryAsync<Device>("select * from  [kauSupport].[dbo].[Devices]");
        if (response.Any())
        {
            return Ok(response); 
        }

        else
        {
            return Ok("No devices found .. "); 
        }
    }
    //-----------------------------------------------------------------------------------------

    [HttpGet]
    [Route("GetLabDevices/ID=" + "{lab_Number}")]
    public async Task<ActionResult> getLabDevices(string lab_Number )
    {
        var response = await conn.QueryAsync<Device>("select * from  [kauSupport].[dbo].[Devices] where deviceLocatedLab = @deviceLocatedLab" , new {deviceLocatedLab = lab_Number});
        return Ok(response); 
    }
    //-----------------------------------------------------------------------------------------

    [HttpPost]
    [Route("AddReport")]
    //TODO Add cockie ... 


    public async Task<ActionResult> addReport(String Device_Number , String Serial_Number, String Device_LocatedLab , String Problem_Description  , String Reported_By)
    {
        DateTime currentDateTime = DateTime.Now.Date;
        string Report_Type = "issue"; 
        await conn.ExecuteAsync("INSERT INTO  [kauSupport].[dbo].[Reports] ( deviceNumber, serialNumber,deviceLocatedLab, reportType , problemDescription, reportedBy , reportDate) values ( @deviceNumber, @serialNumber, @deviceLocatedLab, @reportType , @problemDescription, @reportedBy ,@reportDate ) ", new { deviceNumber= Device_Number, serialNumber= Serial_Number,deviceLocatedLab = Device_LocatedLab, reportType =Report_Type  , problemDescription = Problem_Description, reportedBy= Reported_By , reportDate = currentDateTime });
        string status = "Reported"; 
        await conn.ExecuteAsync( "UPDATE [kauSupport].[dbo].[Devices] SET deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber; ", new {serialNumber = Serial_Number  , deviceStatus = status });
        return Ok(true); 
    }
    //-----------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetReports")]
    public async Task<ActionResult> getReports()
    {
        var response = await conn.QueryAsync<Report>("select * from  [kauSupport].[dbo].[Reports]");
        return Ok(response); 
    }
    
    //-----------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetMyReports")]
    public async Task<ActionResult> getMyReports([Required] string User_Id)
    {
        var response = await conn.QueryAsync<Report>("select * from  [kauSupport].[dbo].[Reports] where reportedBy= @reportedBy" , new{reportedBy = User_Id});
        return Ok(response); 
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
                    LabNumber = lab.labNumber ,
                    ReportedDevicesCount = reportedCount,
                    WorkingDevicesCount = workingCount
                }
                
                );

         

        }

        return Ok(labsWithDeviceCountsList);
    }


   

    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    // This here replacment to "" AddReport "" to add coocki so that a device when reported cant be reported again withn 3 days .... 
    /*
     
       [HttpPost]
[Route("AddReport")]
public async Task<ActionResult> addReport(
    String Device_Number, String Serial_Number, String Device_LocatedLab,
    String Report_Type, String Problem_Description, String Reported_By)
{
    using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    DateTime currentDateTime = DateTime.Now;

    // Check if the device has been reported in the last 3 days
    if (!IsDeviceReportable(Serial_Number))
    {
        return BadRequest("Device cannot be reported again within 3 days.");
    }

    // Insert the report into the Reports table
    await conn.ExecuteAsync(
        "INSERT INTO [kauSupport].[dbo].[Reports] " +
        "(deviceNumber, serialNumber, deviceLocatedLab, reportType, problemDescription, reportedBy, reportDate) " +
        "VALUES (@deviceNumber, @serialNumber, @deviceLocatedLab, @reportType, @problemDescription, @reportedBy, @reportDate)",
        new
        {
            deviceNumber = Device_Number,
            serialNumber = Serial_Number,
            deviceLocatedLab = Device_LocatedLab,
            reportType = Report_Type,
            problemDescription = Problem_Description,
            reportedBy = Reported_By,
            reportDate = currentDateTime
        });

    // Update the device status
    string status = "Reported";
    await conn.ExecuteAsync(
        "UPDATE [kauSupport].[dbo].[Devices] SET deviceStatus = @deviceStatus WHERE deviceNumber = @deviceNumber",
        new { deviceNumber = Device_Number, deviceStatus = status });

    // Set a cookie to mark the reported device
    HttpContext.Response.Cookies.Append("ReportedDevice_" + Serial_Number, currentDateTime.ToString(), new CookieOptions
    {
        Expires = DateTime.Now.AddDays(3) // Set the cookie to expire in 3 days
    });

    return Ok(true);
}
   
     */


    
    
}