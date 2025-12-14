using Learnly.Models;
using Learnly.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface IModuleService
    {
        Task<IEnumerable<Module>> GetModulesByCourseAsync(int courseId);
        Task<Module?> GetModuleByIdAsync(int moduleId);
        Task<Module> CreateModuleAsync(int courseId, ModuleCreateDto moduleDto);
        Task<Module?> UpdateModuleAsync(int moduleId, string title);
        Task<Module?> UpdateModuleThumbnailAsync(int moduleId, string thumbnailPath);
        Task<bool> DeleteModuleAsync(int moduleId);
    }
}
