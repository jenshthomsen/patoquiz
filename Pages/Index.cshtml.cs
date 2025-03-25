using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using patoquiz.Data;
using patoquiz.Models;

namespace patoquiz.Pages
{
    public class IndexModel : PageModel
    {
        private readonly QuizDbContext _context;
        public List<Quiz> Quizzes { get; set; } = new();

        public IndexModel(QuizDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            Quizzes = _context.Quizzes
                .Include(q => q.Subspecialty)
                .ToList();
        }
    }
}
