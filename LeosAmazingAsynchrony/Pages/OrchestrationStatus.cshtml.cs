using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeosAmazingAsynchrony.Pages
{
    public class OrchestrationStatusModel : PageModel
    {
        public OrchestrationQueryResult QueryResult { get; set; }

        public async Task OnGet(string url)
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync(url);
                QueryResult = await result.Content.ReadAsAsync<OrchestrationQueryResult>();
                Response.Headers.Add("X-Orchestration-Status", QueryResult.RuntimeStatus);
            }
        }
    }
}