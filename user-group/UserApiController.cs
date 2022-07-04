using EPiServer.Cms.UI.AspNetIdentity;
using Foundation.Infrastructure.Cms.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Foundation.Custom
{
    [ApiController]
    [Route("user-api")]
    public class UserApiController : ControllerBase
    {
        private readonly ApplicationDbContext<SiteUser> _db;
        public UserApiController(ApplicationDbContext<SiteUser> db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("test-user")]
        public async Task<ActionResult<string>> TestUser([FromQuery] string firstName = null)
        {
            var user = HttpContext.User.Identity;
            var userDb = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower().Equals(user.Name.ToLower()));
            if (!string.IsNullOrEmpty(firstName))
            {
                userDb.FirstName = firstName;
                _db.Users.Update(userDb);
                await _db.SaveChangesAsync();
                userDb = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower().Equals(user.Name.ToLower()));
            }
            return Ok(userDb.FirstName ?? "it's null");
        } 
        
        [HttpGet]
        [Route("create-user")]
        public async Task<ActionResult<string>> CreateUser([FromQuery] string username = null)
        {
            var uiSignInManager = ServiceLocator.Current.GetInstance<UISignInManager>();
            var uiUserProvider = ServiceLocator.Current.GetInstance<UIUserProvider>();
            var uiRoleProvider = ServiceLocator.Current.GetInstance<UIRoleProvider>();
            var uiUserManager = ServiceLocator.Current.GetInstance<UIUserManager>();

            username = username ?? "sysadmin123";
            var email = username + "@episerver.com";
            var password = "P@ssw0rd";
            var log = "";
            string[] roles = { "Administrators" };

            var userList = uiUserProvider.FindUsersByNameAsync(username, 0, 1);
            var user = await userList.FirstOrDefaultAsync();

            if (user == null)
            {

                var result = await uiUserProvider.CreateUserAsync(username, password, email, null, null, true);
                foreach (var error in result.Errors)
                {
                    log += error;
                }
                if (result.Errors.Count() == 0)
                {
                    string role = roles.First();
                    var isExist = await uiRoleProvider.RoleExistsAsync(role);
                    if (!isExist)
                    {
                        await uiRoleProvider.CreateRoleAsync(role);
                    }
                    await uiRoleProvider.AddUserToRolesAsync(username, roles);
                    log += "Created successfully user: " + username;
                }
            }
            else
            {
                log += ("Failed to create as the user " + username + " already exist");
                return Ok(log);
            }
            log += "User name: " + username;
            log += "Password: " + password;
            log += "Email: " + email;

            return Ok(log);
        } 
    }
}
