using Microsoft.Extensions.Caching.Memory;

namespace hlasovanisvj.Services;

public class UploadToMemoryCacheService(ILogger<UploadToMemoryCacheService> logger, IMemoryCache memoryCache)
: IUploadService
{
    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var id = Guid.NewGuid().ToString();
    
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var fileBytes = stream.ToArray();
    
        memoryCache.Set(id, fileBytes);
    
        return id;
    }

    public string GetFilePath(string fileId)
    {
        return fileId;
    }

    public async Task<byte[]> ReadAllBytesAsync(string fileId)
    {
        return memoryCache.Get<byte[]>(fileId);
    }

}