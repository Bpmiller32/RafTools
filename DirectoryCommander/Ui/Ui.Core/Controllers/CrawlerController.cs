using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Ui.Core.Data;

namespace Ui.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrawlerController : ControllerBase
{
    private readonly ILogger<BuilderController> logger;
    private readonly NetworkStreams networkStreams;

    public CrawlerController(ILogger<BuilderController> logger, NetworkStreams networkStreams)
    {
        this.logger = logger;
        this.networkStreams = networkStreams;
    }

    [HttpGet]
    public string Get()
    {
        logger.LogInformation("Crawler Get request recieved");
        return JsonConvert.SerializeObject(networkStreams.CrawlerResponse);
    }
}
