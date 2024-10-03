using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPages.GeoTagsExtractor.CRUD.Models;

namespace RazorPages.GeoTagsExtractor.CRUD.Pages
{
    public class DetailsModel : PageModel
    {
        public FileModel FileById { get; set; } = default!;

        private readonly ApplicationContext _context;
        private readonly IWebHostEnvironment _environment;

        public DetailsModel(ApplicationContext context,IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Files==null)
            {
                return NotFound();
            }

            var imageById = await _context.Files.FirstOrDefaultAsync(image => image.Id == id);

            if (imageById == null) {
                return NotFound();
            }
            else
            {
                FileById = imageById;
            }

            return Page();
        }
    }
}
