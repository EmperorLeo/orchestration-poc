using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace LeosAmazingAsynchrony.Pages
{
    public class OrchestrationStatusModel : PageModel
    {
        public OrchestrationQueryResult QueryResult { get; set; }
        public string ResultsFileUrl;

        public OrchestrationStatusModel(IOptions<AppSettings> options)
        {
            ResultsFileUrl = options.Value.ResultsFileUrl;
        }

        public async Task OnGet(string url)
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync(url);
                QueryResult = await result.Content.ReadAsAsync<OrchestrationQueryResult>();
                Response.Headers.Add("X-Orchestration-Status", QueryResult.RuntimeStatus);
                if (!string.IsNullOrEmpty(QueryResult.Output))
                {
                    Response.Headers.Add("X-Orchestration-Output", $"{ResultsFileUrl}/{QueryResult.Output}");
                }
            }
        }
    }
}