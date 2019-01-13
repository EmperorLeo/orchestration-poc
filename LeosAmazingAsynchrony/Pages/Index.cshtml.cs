using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LeosAmazingAsynchrony.Pages
{
    public class IndexModel : PageModel
    {
        private readonly string _functionsUrl;
        private readonly CloudBlobContainer _cloudBlobContainer;

        public IndexModel(IOptions<AppSettings> options)
        {
            _functionsUrl = options.Value.FunctionsAppUrl;
            var account = CloudStorageAccount.Parse(options.Value.ConnectionStrings.StorageAccountConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            _cloudBlobContainer = blobClient.GetContainerReference("uploadedFiles");
        }

        public void OnGet()
        {

        }

        [BindProperty]
        public IFormFile Upload { get; set; }
        public async Task OnPostAsync()
        {
            if (Upload != null)
            {
                var uniqueId = Guid.NewGuid();
                if (await _cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    var permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };
                    await _cloudBlobContainer.SetPermissionsAsync(permissions);
                };
                var blockBlob = _cloudBlobContainer.GetBlockBlobReference(uniqueId.ToString());
                using (var stream = Upload.OpenReadStream())
                {
                    await blockBlob.UploadFromStreamAsync(stream);
                }
                using (var client = new HttpClient())
                {
                    var result = await client.GetAsync(_functionsUrl);
                    var orchestrationStartResult = await result.Content.ReadAsAsync<OrchestrationStartResult>();
                    Console.WriteLine(orchestrationStartResult.Id);
                    ViewData.Add("OrchestrationResult", orchestrationStartResult);
                }
                TempData.Add("FileName", Upload.FileName);
            }
        }
    }
}
