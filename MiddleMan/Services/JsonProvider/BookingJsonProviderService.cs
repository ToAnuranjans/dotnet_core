namespace MiddleMan.Services.JsonProvider;

public class BookingJsonProviderService(IHostEnvironment environment) : IJsonProviderService
{
    private readonly string _dataPath = Path.Combine(environment.ContentRootPath, "Data");

    public string GetJson(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("File name is required.", nameof(key));
        }

        var fileName = Path.GetFileName(key);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != key)
        {
            throw new ArgumentException("Only file names from the Data folder are allowed.", nameof(key));
        }

        if (!Path.HasExtension(fileName))
        {
            fileName = $"{fileName}.json";
        }

        var filePath = Path.Combine(_dataPath, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Json file '{fileName}' was not found in the Data folder.", filePath);
        }

        return File.ReadAllText(filePath);
    }
}
