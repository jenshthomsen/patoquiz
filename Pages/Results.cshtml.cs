using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using patoquiz.Data;
using patoquiz.Models;
using patoquiz.Extensions;


namespace patoquiz.Pages
{
    public class ResultsModel : PageModel
    {
        private readonly QuizDbContext _context;
        public Quiz Quiz { get; set; } = null!;
        public List<string> UserAnswers { get; set; } = new();
        public int Score { get; set; }

        public ResultsModel(QuizDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int id)
        {
            Quiz = _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefault(q => q.Id == id);

            if (Quiz == null || Quiz.Questions == null)
            {
                return RedirectToPage("/Error");
            }

            Quiz.Questions ??= new List<Question>();
            foreach (var question in Quiz.Questions)
            {
                question.Answers ??= new List<Answer>();
            }

            UserAnswers = HttpContext.Session.GetObject<List<string>>($"Quiz_{id}_Answers") ?? new List<string>();
            Score = Quiz.Questions.Zip(UserAnswers, (q, ua) => q.Answers.Any(a => a.Text == ua && a.IsCorrect) ? 1 : 0).Sum();

            return Page();
        }
    }
}