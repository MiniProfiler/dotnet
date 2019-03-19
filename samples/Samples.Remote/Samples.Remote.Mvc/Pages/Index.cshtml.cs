using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Samples.Remote.Mvc.Client;

namespace Samples.Remote.Mvc.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SamplesApiHttpClient _client;

        public IndexModel(SamplesApiHttpClient client)
        {
            _client = client;
        }

        public void OnGet()
        {

        }

        public async Task OnPostAllAsync()
        {
            Samples = await _client.GetAllAsync();
        }

        public async Task OnPostOddAsync()
        {
            Samples = await _client.GetOddAsync();
        }

        public JArray Samples { get; private set; }
    }
}
