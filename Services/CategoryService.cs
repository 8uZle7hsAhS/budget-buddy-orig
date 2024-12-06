// Services/CategoryService.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Budget_Baddie.Models;
using Microsoft.EntityFrameworkCore;

namespace Budget_Baddie.Services
{
    public class CategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<List<Category>> GetCategoriesByTypeAsync(string type)
        {
            return await _context.Categories
                .Where(c => c.Type.ToLower() == type.ToLower())
                .ToListAsync();
        }
    }
}