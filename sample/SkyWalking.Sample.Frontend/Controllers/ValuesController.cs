using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SkyWalking.Sample.Frontend.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet("/values")]
        public async Task<IEnumerable<string>> Get()
        {
            var task1= new HttpClient().GetAsync("http://ocs.aihuishou.com/home/healthcheck");
            var task2= new HttpClient().GetAsync("http://ocs.aihuishou.com/home/healthcheck");
            await Task.WhenAll(task1, task2);
            return new string[] { "value1", "value2" };
        } 
    }
}