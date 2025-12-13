using Learnly.Models;
using Learnly.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public interface IModuleService
    {
        Task<IEnumerable<Module>> GetModulesByCourseAsync(int courseId);
        Task<Module> CreateModuleAsync(int courseId, ModuleCreateDto moduleDto);
    }
}
