using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Ui.Core.Data;

namespace Ui.Core.Services;

public class SocketMessageInputFormatter : InputFormatter
{
    private const string ContentType = "application/json";

    public SocketMessageInputFormatter()
    {
        SupportedMediaTypes.Add(ContentType);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        var request = context.HttpContext.Request;
        
        string responseSerialized;
        using (StreamReader reader = new StreamReader(request.Body))
        {
            responseSerialized = await reader.ReadToEndAsync();
            SocketMessage message = JsonConvert.DeserializeObject<SocketMessage>(responseSerialized);
            return await InputFormatterResult.SuccessAsync(message);
        }
    }

    public override bool CanRead(InputFormatterContext context)
    {
        var contentType = context.HttpContext.Request.ContentType;
        return contentType.StartsWith(ContentType);
    }
}