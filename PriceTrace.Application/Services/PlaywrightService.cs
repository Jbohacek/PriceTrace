using Microsoft.Playwright;

namespace PriceTrace.Application.Services;

public interface IPlaywrightService : IAsyncDisposable
{
    Task<IPage> GetPageAsync();
    Task InitializeAsync();
}

public class PlaywrightService : IPlaywrightService
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isInitialized;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _semaphore.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            _context = await _browser.NewContextAsync();
            _isInitialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IPage> GetPageAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        return await _context!.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }
}
