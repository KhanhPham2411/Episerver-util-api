using EPiServer.Cms.UI.AspNetIdentity;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Foundation.Custom
{
    [ApiController]
    [Route("user-api")]
    public class UserApiController : ControllerBase
    {
        private readonly ApplicationDbContext<ApplicationUser> _db;
        public UserApiController(ApplicationDbContext<ApplicationUser> db)
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
                userDb.UserName = firstName;
                _db.Users.Update(userDb);
                await _db.SaveChangesAsync();
                userDb = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower().Equals(user.Name.ToLower()));
            }
            return Ok(userDb.UserName ?? "it's null");
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
