using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface ILessonService
    {
        Task<IEnumerable<Lesson>> GetLessonsByModuleAsync(int moduleId);
        Task<Lesson> CreateLessonAsync(int moduleId, LessonCreateDto lessonDto);
        Task<string> UploadVideoAsync(int lessonId, IFormFile videoFile);
        Task<Lesson> GetLessonByIdAsync(int lessonId);
        Task<Lesson?> UpdateLessonAsync(int lessonId, string title);
        Task<Lesson?> UpdateLessonThumbnailAsync(int lessonId, string thumbnailPath);
        Task<bool> DeleteLessonAsync(int lessonId);
    }
}
