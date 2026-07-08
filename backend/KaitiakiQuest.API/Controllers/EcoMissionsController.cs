using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace KaitiakiQuest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EcoMissionsController : ControllerBase
    {

    }
}
