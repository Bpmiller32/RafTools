namespace Tester
{
    public class Settings
    {
        public string Name { get; set; }
        public string DiscDrivePath { get; set; }
        public string DongleId { get; set; }

        public void Validate(IConfiguration config)
        {
            // Check that appsettings.json exists at all
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")))
            {
                throw new Exception("appsettings.json is missing, make sure there is a valid appsettings.json file in the same directory as the application");
            }
            // Dongle checks
            if (string.IsNullOrEmpty(config.GetValue<string>("settings:DongleId")))
            {
                throw new Exception("No dongle id provided, check appsettings.json");
            }

            // Set dongleId
            DongleId = config.GetValue<string>("settings:DongleId");

            // Verify for each directory
            foreach (string dir in new List<string>() { "SmartMatch", "Parascript", "RoyalMail" })
            {
                if (dir == Name)
                {
                    // DiscDrivePath checks
                    if (string.IsNullOrEmpty(config.GetValue<string>("settings:" + dir + ":DiscDrivePath")))
                    {
                        throw new Exception("No path to disc drive provided, check appsettings.json");
                    }
                    else
                    {
                        DiscDrivePath = config.GetValue<string>("settings:" + dir + ":DiscDrivePath");
                    }
                    // Check if provided directory exists on the filesystem
                    if (!Directory.Exists(DiscDrivePath))
                    {
                        throw new Exception("Disc drive provided doesn't exist: " + DiscDrivePath);
                    }
                    // Check that there are files in the provided directory
                    if (!Directory.EnumerateFileSystemEntries(DiscDrivePath).Any())
                    {
                        throw new Exception("Disc drive provided is empty: " + DiscDrivePath);
                    }
                }
            }
        }
    }
}