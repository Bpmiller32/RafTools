using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Ui.Core.Data;
using Ui.Core.Services;

namespace Ui.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuilderController : ControllerBase
{
    private readonly ILogger<BuilderController> logger;
    private readonly NetworkStreams networkStreams;

    public BuilderController(ILogger<BuilderController> logger, NetworkStreams networkStreams)
    {
        this.logger = logger;
        this.networkStreams = networkStreams;
    }

    [HttpGet]
    public string Get()
    {
        logger.LogInformation("Get request recieved");
        return JsonConvert.SerializeObject(networkStreams.BuilderResponse);
    }

    [HttpPost]
    [Consumes("application/json")]
    public string Post([FromBody]SocketMessage message)
    {
        string serializedObject = JsonConvert.SerializeObject(message);
        byte[] data = Encoding.UTF8.GetBytes(serializedObject);
        networkStreams.BuilderStream.Write(data);

        return "Message sent";
    }
}
