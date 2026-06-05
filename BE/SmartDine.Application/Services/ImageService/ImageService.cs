
using Microsoft.Extensions.Configuration;
using Supabase;
using SupabaseClient = Supabase.Client;
namespace SmartDine.Application.Services.ImageService
{
    public class ImageService
    {
        private readonly IConfiguration _configuration;


        public ImageService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task<string> UploadImageAsync(
      Stream imageStream,
      string fileName)
        {
            var url = _configuration["Supabase:Url"];
            var apiKey = _configuration["Supabase:ApiKey"];
            var bucketName = _configuration["Supabase:BucketName"];

            var client = new Client(
                url,
                apiKey,
                new SupabaseOptions());

            await client.InitializeAsync();

            var path = $"{Guid.NewGuid()}-{fileName}";

            byte[] fileBytes;

            using (var memoryStream = new MemoryStream())
            {
                await imageStream.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            await client.Storage
                .From(bucketName)
                .Upload(fileBytes, path);

            return client.Storage
                .From(bucketName)
                .GetPublicUrl(path);
        }
    }
}
