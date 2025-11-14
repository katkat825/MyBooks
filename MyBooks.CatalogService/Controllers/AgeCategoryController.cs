using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using MyBooks.CatalogService.Models;
using MyBooks.CatalogService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace MyBooks.CatalogService.Controllers
{
    [Route("age-ratings")]
    [ApiController]
    [AllowAnonymous]
    public class AgeCategoryController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        public AgeCategoryController(CatalogDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgeCategory>>> GetAgeCategories()
        {
            return await _context.AgeCategories.ToListAsync();
        }
    }
}
