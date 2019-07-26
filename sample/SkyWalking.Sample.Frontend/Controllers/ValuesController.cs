using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SkyWalking.Sample.Frontend.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ErpDbContext _db;
        public ValuesController(ErpDbContext db)
        {
            _db = db;
        }
        // GET api/values
        [HttpGet("/values")]
        public async Task<IActionResult> Get()
        {
            var note = await _db.NotebookAppReleaseNotes.OrderByDescending(n => n.CreateDt).FirstOrDefaultAsync();
            return Json(note);
        } 
    }
}