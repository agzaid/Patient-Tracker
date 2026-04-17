using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PatientTracker.Application.Services;

public interface IImageProcessingService
{
    Task<(string filePath, string? thumbnailPath, int width, int height)> ProcessImageAsync(
        Stream imageStream, 
        string fileName, 
        string userFolder,
        int? maxWidth = null,
        int? maxHeight = null,
        int thumbnailSize = 200);
}

public class ImageProcessingService : IImageProcessingService
{
    private readonly IConfiguration _configuration;
    private readonly string _uploadsPath;

    public ImageProcessingService(IConfiguration configuration)
    {
        _configuration = configuration;
        _uploadsPath = configuration["Uploads:Path"] ?? "uploads";
    }

    public async Task<(string filePath, string? thumbnailPath, int width, int height)> ProcessImageAsync(
        Stream imageStream,
        string fileName,
        string userFolder,
        int? maxWidth = null,
        int? maxHeight = null,
        int thumbnailSize = 200)
    {
        // Create user directory if it doesn't exist
        var userDirectory = Path.Combine(_uploadsPath, userFolder);
        Directory.CreateDirectory(userDirectory);

        // Load the image
        using var image = await Image.LoadAsync(imageStream);
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        // Calculate new dimensions if specified
        int newWidth = originalWidth;
        int newHeight = originalHeight;

        if (maxWidth.HasValue || maxHeight.HasValue)
        {
            newWidth = maxWidth ?? originalWidth;
            newHeight = maxHeight ?? originalHeight;

            // Maintain aspect ratio
            var aspectRatio = (double)originalWidth / originalHeight;
            if (maxWidth.HasValue && !maxHeight.HasValue)
            {
                newHeight = (int)(newWidth / aspectRatio);
            }
            else if (!maxWidth.HasValue && maxHeight.HasValue)
            {
                newWidth = (int)(newHeight * aspectRatio);
            }
            else if (maxWidth.HasValue && maxHeight.HasValue)
            {
                var widthRatio = (double)maxWidth.Value / originalWidth;
                var heightRatio = (double)maxHeight.Value / originalHeight;
                var ratio = Math.Min(widthRatio, heightRatio);
                newWidth = (int)(originalWidth * ratio);
                newHeight = (int)(originalHeight * ratio);
            }
        }

        // Resize the main image
        if (newWidth != originalWidth || newHeight != originalHeight)
        {
            image.Mutate(x => x.Resize(newWidth, newHeight));
        }

        // Save as WebP
        var webpEncoder = new WebpEncoder
        {
            Quality = _configuration.GetValue<int>("Uploads:ImageQuality", 80)
        };

        var fileExtension = Path.GetExtension(fileName);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var webpFileName = $"{fileNameWithoutExt}_{Guid.NewGuid()}.webp";
        var mainFilePath = Path.Combine(userDirectory, webpFileName);

        await image.SaveAsync(mainFilePath, webpEncoder);

        // Create thumbnail
        string? thumbnailPath = null;
        if (thumbnailSize > 0 && (originalWidth > thumbnailSize || originalHeight > thumbnailSize))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(thumbnailSize, thumbnailSize),
                Mode = ResizeMode.Max
            }));

            var thumbnailFileName = $"{fileNameWithoutExt}_thumb_{Guid.NewGuid()}.webp";
            thumbnailPath = Path.Combine(userDirectory, thumbnailFileName);
            await image.SaveAsync(thumbnailPath, webpEncoder);
        }

        return (mainFilePath, thumbnailPath, newWidth, newHeight);
    }
}
