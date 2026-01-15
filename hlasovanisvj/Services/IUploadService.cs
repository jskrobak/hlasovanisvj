using Microsoft.AspNetCore.Mvc;

namespace hlasovanisvj.Services;

public interface IUploadService
{
    public Task<string> SaveFileAsync([FromForm] IFormFile file);
    string GetFilePath(string fileId);
    public Task<byte[]> ReadAllBytesAsync(string fileId);

}