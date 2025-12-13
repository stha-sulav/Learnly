using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Learnly.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Learnly.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly ILogger<FileCleanupService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        public FileCleanupService(ILogger<FileCleanupService> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOrphanedFiles();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up orphaned files.");
                }

                // Wait for 24 hours before running again
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }

            _logger.LogInformation("File Cleanup Service is stopping.");
        }

        private async Task CleanupOrphanedFiles()
        {
            _logger.LogInformation("Starting orphaned files cleanup job.");

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

                var videosPath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "videos");
                if (!Directory.Exists(videosPath))
                {
                    _logger.LogWarning("Videos directory not found. Skipping cleanup.");
                    return;
                }

                var allVideoFiles = Directory.GetFiles(videosPath, "*", SearchOption.AllDirectories)
                                             .Select(p => Path.GetFileName(p));

                var referencedVideoFiles = dbContext.Lessons
                                                    .Where(l => !string.IsNullOrEmpty(l.VideoPath))
                                                    .Select(l => Path.GetFileName(l.VideoPath))
                                                    .ToList();

                var orphanedFiles = allVideoFiles.Except(referencedVideoFiles).ToList();

                if (orphanedFiles.Any())
                {
                    _logger.LogWarning($"Found {orphanedFiles.Count} orphaned files:");
                    foreach (var orphan in orphanedFiles)
                    {
                        _logger.LogWarning($"Orphaned file found: {orphan}");
                    }

                    if (_configuration.GetValue<bool>("FileCleanup:DeleteOrphanedFiles"))
                    {
                        _logger.LogInformation("Deleting orphaned files as configured.");
                        foreach (var orphan in orphanedFiles)
                        {
                            try
                            {
                                var filePath = Path.Combine(videosPath, orphan);
                                File.Delete(filePath);
                                _logger.LogInformation($"Deleted orphaned file: {orphan}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error deleting orphaned file: {orphan}");
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No orphaned files found.");
                }
            }

            await Task.CompletedTask;
        }
    }
}
