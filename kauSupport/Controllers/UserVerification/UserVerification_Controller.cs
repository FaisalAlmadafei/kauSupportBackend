using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using Dapper;
using kauSupport.Connection;
using kauSupport.Models;
using Microsoft.AspNetCore.Mvc;

namespace kauSupport.Controllers.UserVerification;

[Route("api/[controller]")]
[ApiController]
public class UserVerification_Controller : Controller
{
    private readonly IDbConnectionFactory _dbConnectionFactory;


    public UserVerification_Controller(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory; // Instance of SqlConnectionFactory came form dependency injection 
    }

    //------------------------------------------------------------------------------------------------------------------

    [HttpPost]
    [Route("LogIn")]
    public async Task<IActionResult> LogIn([Required] string User_Id, [Required] string Password)
    {
        var conn = _dbConnectionFactory.CreateConnection();

        // Retrieve the user by User_Id
        var userPass = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT password FROM [kauSupport].[dbo].[Users] WHERE userId = @UserId",
            new { UserId = User_Id });

        // Check if user exists and then verify the password
        if (userPass != null && BCrypt.Net.BCrypt.Verify(Password, userPass))
        {
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                "select UserId, firstName, lastName, role,email from  [kauSupport].[dbo].[Users] WHERE UserId = @UserId",
                new { UserId = User_Id });
            return Ok(user);
        }
        else
        {
            return BadRequest("Invalid user ID or password.");
        }
    }
    //------------------------------------------------------------------------------------------------------------------

   
    //-----------------------------------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetUsers")]
    public async Task<ActionResult> getUsers()
    {
        var conn = _dbConnectionFactory.CreateConnection();

        var users = await conn.QueryAsync<User>(
            "select UserId, firstName, lastName, role,email  from   [kauSupport].[dbo].[Users]");
        if (users.Any())
        {
            return Ok(users);
        }
        else
        {
            return BadRequest("No users found..");
        }
    }
    //------------------------------------------------------------------------------------------------------------------

    [HttpGet]
    [Route("GetTechnicalMembers")]
    public async Task<ActionResult> GetTechnicalMembers()
    {
        var conn = _dbConnectionFactory.CreateConnection();

        var users = await conn.QueryAsync<User>(
            "select UserId, firstName, lastName, role,email  from   [kauSupport].[dbo].[Users] WHERE role= @role",
            new
            {
                role = "Technical Member"
            });
        if (users.Any())
        {
            return Ok(users);
        }
        else
        {
            return BadRequest("No users found..");
        }
    }

    //------------------------------------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetUserById")]
    public async Task<ActionResult> getUser([Required] String user_Id)
    {
        var conn = _dbConnectionFactory.CreateConnection();

        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "select UserId, firstName, lastName, role,email from  [kauSupport].[dbo].[Users] WHERE UserId = @UserId",
            new { UserId = user_Id });
        if (user != null)
        {
            return Ok(user);
        }
        else
        {
            return BadRequest("User Not found");
        }
    }
}