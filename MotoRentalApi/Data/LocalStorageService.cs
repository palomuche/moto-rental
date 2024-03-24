namespace MotoRentalApi.Data
{
    public class LocalStorageService
    {
        private readonly string _storageDirectory;

        public LocalStorageService(IWebHostEnvironment webHostEnvironment)
        {
            _storageDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "Storage");
        }

        public async Task<string> UploadPhoto(IFormFile file)
        {
            // Verifica se o diretório de armazenamento existe, se não, cria
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }

            // Gera um nome de arquivo único para evitar colisões
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // Combina o caminho completo do arquivo
            var filePath = Path.Combine(_storageDirectory, fileName);

            // Salva o arquivo no disco
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }

        public async Task<byte[]> GetFileBytes(string filePath)
        {
            // Lê os bytes do arquivo no disco
            return await File.ReadAllBytesAsync(filePath);
        }
    }
}
