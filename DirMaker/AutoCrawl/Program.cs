using System.Text;
using System.Text.Json;

try
{
    if (string.IsNullOrEmpty(Environment.GetCommandLineArgs()[1]))
    {
        throw new Exception();
    }
}
catch (Exception)
{
    Console.WriteLine("Usage: AutoRun.exe <base api url>");
    Console.WriteLine("Example: AutoRun.exe http://localhost:5000/");
    return;
}

// URL to which you want to send the POST request
string url = Environment.GetCommandLineArgs()[1];

// Data to send in the POST request
string postData = JsonSerializer.Serialize(new { moduleCommand = "start" });
List<string> crawlerList = ["smartmatch", "parascript", "royalmail"];


// Create HttpClient instance
using (HttpClient client = new())
{
    foreach (string crawlerName in crawlerList)
    {
        try
        {
            // Send POST request
            HttpResponseMessage response = await client.PostAsync(url + crawlerName + "/crawler", new StringContent(postData, Encoding.UTF8, "application/json"));

            // Check if the response is successful (status code 200-299)
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Successfully called Crawler: {crawlerName}");
            }
            else
            {
                Console.WriteLine($"Failed to make POST request. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            return;
        }
    }
}