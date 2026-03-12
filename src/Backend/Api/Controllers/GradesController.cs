using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/submissions/{submissionId}/grade")]
    public class GradesController : ControllerBase
    {
        [HttpGet]
        public IActionResult CreateGrade(Guid submissionId)
        {
            return Ok();
        }
    }
}
