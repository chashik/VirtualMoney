using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace VirtualMoney.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class TransactionsController : ControllerBase
    {
        private readonly Repository _repository;
        public TransactionsController(Repository repository) => _repository = repository;

        // GET: api/Transactions/login - transaction history
        [HttpGet("{login}")]
        public async Task<IActionResult> GetTransactions([FromRoute] string login) =>
            await Task.Run<IActionResult>(() => Ok(_repository.Transactions(login)));

        // POST: api/Transactions
        [HttpPost]
        public async Task<IActionResult> PostTransaction([FromBody] Order order) =>

            await Task.Run<IActionResult>(() =>
            {
                if (_repository.TryTransaction(order, out Transaction transaction))
                    return Ok(transaction); // using 200 status code instead of 201 to omit transaction identification
                else
                    return BadRequest("Invalid order!");
            });

    }
}
