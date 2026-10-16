using Microsoft.AspNetCore.Mvc;

namespace  temp_asp.net_webapi.Controllers;

[ApiController]
[Route("/")]
public class RootController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "root controller endpoint";
    }
}