using System.Data.SqlClient;
using Dapper;
using kauSupport.Models;
using Microsoft.AspNetCore.Mvc;

namespace kauSupport.Controllers.TechnicalSupport;

public class TechnicalSupervisor_Controller : Controller
{
    private readonly IConfiguration config;

    public TechnicalSupervisor_Controller(IConfiguration config)
    {
        this.config = config;
    }

    [HttpGet]
    [Route("GetReports")]
    public async Task<ActionResult> getReports()
    {
        using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        var response = await conn.QueryAsync<Report>("select * from  [kauSupport].[dbo].[Reports]");
        return Ok(response);
    }

    //------------------------------------------------------------------------------------------------------------------
    [HttpPost]
    [Route("AssignTask")]
    public async Task<ActionResult> AssignTask(string User_Id, int Report_Id)
    {
        using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
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
            return Ok();
        }
        else
        {
            return BadRequest("No rows affected");
        }
    }
    //------------------------------------------------------------------------------------------------------------------

    [HttpGet]
    [Route("GetNewReportsForSupervisor")]
    public async Task<ActionResult> GetNewReportsForSupervisor()
    {
        var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        DateTime currentDateTime = DateTime.Now.Date;
        DateTime newPeriodicMaintenanceDate = currentDateTime.AddMonths(6);
        Console.WriteLine(currentDateTime);
        //First we will store all devices with perodic data is today ...
        var devices = await conn.QueryAsync<Device>(
            "select * from  [kauSupport].[dbo].[Devices] where nextPeriodicDate= @nextPeriodicDate",
            new { nextPeriodicDate = currentDateTime });

        // Now we will loop each device and create a new report
        foreach (var device in devices)
        {
            int Device_Number = device.deviceNumber;
            string Serial_Number = device.serialNumber;
            string Device_LocatedLab = device.deviceLocatedLab;
            string Report_Type = "Periodic maintenance";
            string Problem_Description = "The device needs a Periodic maintenance";
            string Reported_By = "System";

            // here we update the nextPeriodicDate
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Devices] SET nextPeriodicDate = @nextPeriodicDate WHERE deviceNumber = @deviceNumber",
                new { nextPeriodicDate = newPeriodicMaintenanceDate, deviceNumber = Device_Number });
            string status = "Reported";
            await conn.ExecuteAsync(
                "UPDATE [kauSupport].[dbo].[Devices] SET deviceStatus = @deviceStatus WHERE serialNumber = @serialNumber; ",
                new { serialNumber = Serial_Number, deviceStatus = status });

            // we add a new report to reports table..
            await conn.ExecuteAsync(
                "INSERT INTO  [kauSupport].[dbo].[Reports] ( deviceNumber, serialNumber,deviceLocatedLab, reportType , problemDescription, reportedBy , reportDate) values ( @deviceNumber, @serialNumber, @deviceLocatedLab, @reportType , @problemDescription, @reportedBy ,@reportDate ) ",
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
    //------------------------------------------------------------------------------------------------------------------

    [HttpGet]
    [Route("MonitorReports")]
    public async Task<ActionResult> MonitorReports()
    {
        using var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        string Report_Status = "in process";
        var response = await conn.QueryAsync<Report>(
            "select * from  [kauSupport].[dbo].[Reports] where reportStatus= @reportStatus",
            new
            {
                reportStatus = Report_Status
            });
        return Ok(response);
    }
}