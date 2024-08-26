using Microsoft.Extensions.Logging;

public class SampleService
{
    private readonly ILogger<SampleService> _logger;

    public SampleService(ILogger<SampleService> logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("This is an informational message.");
        _logger.LogError("This is an error message.");
    }
}
