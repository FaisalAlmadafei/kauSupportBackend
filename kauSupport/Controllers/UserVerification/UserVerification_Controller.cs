using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using Dapper;
using kauSupport.Models;
using Microsoft.AspNetCore.Mvc;

namespace kauSupport.Controllers.UserVerification;

[Route("api/[controller]")]
[ApiController]
public class UserVerification_Controller : Controller
{
    private readonly IConfiguration config;
    private SqlConnection conn;


    public UserVerification_Controller(IConfiguration config)
    {
        this.config = config;
        conn = conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
    }

    //------------------------------------------------------------------------------------------------------------------

    [HttpPost]
    [Route("LogIn")]
    public async Task<IActionResult> LogIn([Required] string User_Id, [Required] string Password)
    {
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

    [HttpPut]
    [Route("AddPass")]
    public async Task<IActionResult> AddPass(string User_Id, string Password)
    {
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

        // Define the SQL query to update the password for a user
        string sqlQuery = "UPDATE [kauSupport].[dbo].[Users] SET password = @password WHERE UserId = @UserId";

        // Provide parameters for the query
        var parameters = new
        {
            UserId = User_Id,
            password = hashedPassword
        };


        await conn.ExecuteAsync(sqlQuery, parameters);
        return Ok("Password updated successfully.");
    }

    //-----------------------------------------------------------------------------------------------------------------
    [HttpGet]
    [Route("GetUsers")]
    public async Task<ActionResult> getUsers()
    {
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