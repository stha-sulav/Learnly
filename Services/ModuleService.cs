using Learnly.Data;
using Learnly.Models;
using Learnly.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learnly.Services
{
    public class ModuleService : IModuleService
    {
        private readonly ApplicationDbContext _context;

        public ModuleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Module>> GetModulesByCourseAsync(int courseId)
        {
            return await _context.Modules
                                 .Where(m => m.CourseId == courseId)
                                 .OrderBy(m => m.OrderIndex)
                                 .ToListAsync();
        }

        public async Task<Module> CreateModuleAsync(int courseId, ModuleCreateDto moduleDto)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return null;
            }

            var module = new Module
            {
                Title = moduleDto.Title,
                OrderIndex = moduleDto.OrderIndex,
                CourseId = courseId,
                ThumbnailPath = moduleDto.ThumbnailPath
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            return module;
        }

        public async Task<Module?> GetModuleByIdAsync(int moduleId)
        {
            return await _context.Modules.FindAsync(moduleId);
        }

        public async Task<Module?> UpdateModuleAsync(int moduleId, string title)
        {
            var module = await _context.Modules.FindAsync(moduleId);
            if (module == null)
            {
                return null;
            }

            module.Title = title;
            await _context.SaveChangesAsync();

            return module;
        }

        public async Task<Module?> UpdateModuleThumbnailAsync(int moduleId, string thumbnailPath)
        {
            var module = await _context.Modules.FindAsync(moduleId);
            if (module == null)
            {
                return null;
            }

            module.ThumbnailPath = thumbnailPath;
            await _context.SaveChangesAsync();

            return module;
        }

        public async Task<bool> DeleteModuleAsync(int moduleId)
        {
            var module = await _context.Modules.FindAsync(moduleId);
            if (module == null)
            {
                return false;
            }

            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
