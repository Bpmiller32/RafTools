Console.WriteLine("Hello, World!");

async Task SendDiscordMessage(string message)
{
    using HttpClient httpClient = new();
    Dictionary<string, string> postValues = new()
        {
            { "content", message + " <@148237136288546816>" }
        };

    FormUrlEncodedContent httpContent = new(postValues);

    await httpClient.PostAsync("https://discord.com/api/webhooks/799379913458843710/XytHRu3A8dX-1hXWvVvGKUBRjnf43rWbkcn4OoTacVAxzDaCEtYqRs4hxS91HVN53-J0", httpContent);
}

async Task<bool> CheckFile(string filePath)
{
    DateTime lastWriteTime = new FileInfo(filePath).LastWriteTime;
    DateTime now = DateTime.Now;

    if ((now - lastWriteTime).TotalMinutes <= 1)
    {
        System.Console.WriteLine("File has been changed");
        await SendDiscordMessage("File has been changed");
        return true;
    }

    return false;
}

while (true)
{
    bool isFinished = false;

    if (File.Exists(@"C:\ProgramData\RAF\ArgosyPost\Save\00201889.dr"))
    {
        isFinished = await CheckFile(@"C:\ProgramData\RAF\ArgosyPost\Save\00201889.dr");
    }
    if (isFinished)
    {
        break;
    }

    if (File.Exists(@"C:\ProgramData\RAF\ArgosyPost\Save\00201889_A1.dr"))
    {
        isFinished = await CheckFile(@"C:\ProgramData\RAF\ArgosyPost\Save\00201889_A1.dr");
    }
    if (isFinished)
    {
        break;
    }

    if (File.Exists(@"C:\ProgramData\RAF\ArgosyPost\Save\00201889_A2.dr"))
    {
        isFinished = await CheckFile(@"C:\ProgramData\RAF\ArgosyPost\Save\00201889_A2.dr");
    }
    if (isFinished)
    {
        break;
    }

    await Task.Delay(TimeSpan.FromSeconds(60));
}
