using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace VirtualMoney.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly Repository _repository;
        public UsersController(Repository repository) => _repository = repository;

        // GET: api/Users - all users
        [HttpGet]
        public async Task<IActionResult> GetUsers() =>
            await Task.Run<IActionResult>(() => Ok(_repository.Users));

        // POST: api/Users - new user
        [HttpPost]
        public async Task<IActionResult> PostUser([FromBody] User user) =>
            
            await Task.Run<IActionResult>(() =>
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (_repository.CreateUser(user.Login, user.SecretHash))
                    return Ok(new User { Login = user.Login }); // using 200 status code instead of 201 
                else
                    return BadRequest("Login is used");
            });
    }
}