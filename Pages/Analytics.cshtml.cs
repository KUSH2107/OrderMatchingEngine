using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class AnalyticsModel : PageModel
    {
        private readonly ILogger<AnalyticsModel> _logger;

        public AnalyticsModel(ILogger<AnalyticsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
