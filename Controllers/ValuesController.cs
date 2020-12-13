using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RequestLogMiddleware.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {

        [HttpGet]
        public ActionResult Get()
        {
            return Ok("GET Success!");
        }

        [HttpPost]
        public ActionResult Post()
        {
            return Ok("POST Success!");
        }

    }
}
