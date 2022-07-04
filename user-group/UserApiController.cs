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
    }
}