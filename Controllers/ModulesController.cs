using Learnly.Models;
using Learnly.Services;
using Learnly.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Learnly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly IModuleService _moduleService;

        public ModulesController(IModuleService moduleService)
        {
            _moduleService = moduleService;
        }

        // GET: api/Modules/ByCourse/5
        [HttpGet("ByCourse/{courseId}")]
        public async Task<ActionResult<IEnumerable<Module>>> GetModulesByCourse(int courseId)
        {
            var modules = await _moduleService.GetModulesByCourseAsync(courseId);
            return Ok(modules);
        }

        // POST: api/Modules/ByCourse/5
        [HttpPost("ByCourse/{courseId}")]
        [Authorize(Roles = "Instructor,Admin")]
        public async Task<ActionResult<Module>> PostModule(int courseId, ModuleCreateDto moduleDto)
        {
            var module = await _moduleService.CreateModuleAsync(courseId, moduleDto);

            if (module == null)
            {
                return NotFound("Course not found.");
            }

            return CreatedAtAction(nameof(GetModulesByCourse), new { courseId = module.CourseId }, module);
        }
    }
}
