namespace MotoRentalApi.Data
{
    public class LocalStorageService
    {
        private readonly string _storageDirectory;

        public LocalStorageService(IWebHostEnvironment webHostEnvironment)
        {
            _storageDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "Storage");
        }

        public string UploadPhoto(IFormFile file)
        {
            // Checks if the storage directory exists, if not, creates it
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }

            // Generates a unique file name to avoid collisions
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // Combines the full file path
            var filePath = Path.Combine(_storageDirectory, fileName);

            // Saves the file to disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyToAsync(stream);
            }

            return filePath;
        }

        public byte[] GetFileBytes(string filePath)
        {
            // Reads the file bytes from disk
            return File.ReadAllBytes(filePath);
        }
    }
}
