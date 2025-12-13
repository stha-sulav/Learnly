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
                CourseId = courseId
            };

            _context.Modules.Add(module);
            await _context.SaveChangesAsync();

            return module;
        }
    }
}
