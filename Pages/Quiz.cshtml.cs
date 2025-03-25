using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using patoquiz.Data;
using patoquiz.Extensions;
using patoquiz.Models;


namespace patoquiz.Pages
{
    public class QuizModel : PageModel
    {
        private readonly QuizDbContext _context;
        public Quiz Quiz { get; set; } = null!;
        public Question CurrentQuestion { get; set; } = null!;
        public int CurrentQuestionIndex { get; set; }
        public List<string> UserAnswers { get; set; } = new(); // Track user selections

        public QuizModel(QuizDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int id, int questionIndex = 0)
        {
            Quiz = _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .Include(q => q.Subspecialty)
                .FirstOrDefault(q => q.Id == id);

            if (Quiz == null || Quiz.Questions == null || !Quiz.Questions.Any())
            {
                return RedirectToPage("/Error");
            }

            Quiz.Questions ??= new List<Question>();
            foreach (var question in Quiz.Questions)
            {
                question.Answers ??= new List<Answer>();
            }

            if (questionIndex < 0 || questionIndex >= Quiz.Questions.Count)
            {
                return RedirectToPage("/Results", new { id }); // Quiz done, go to results
            }

            CurrentQuestionIndex = questionIndex;
            CurrentQuestion = Quiz.Questions[questionIndex];

            // Load user answers from session (or initialize if empty)
            UserAnswers = HttpContext.Session.GetObject<List<string>>($"Quiz_{id}_Answers") ?? new List<string>(new string[Quiz.Questions.Count]);
            return Page();
        }

        public IActionResult OnPost(int id, int questionIndex, string selectedAnswer)
        {
            Quiz = _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefault(q => q.Id == id);

            if (Quiz == null || Quiz.Questions == null || questionIndex >= Quiz.Questions.Count)
            {
                return RedirectToPage("/Error");
            }

            // Save user's answer
            UserAnswers = HttpContext.Session.GetObject<List<string>>($"Quiz_{id}_Answers") ?? new List<string>(new string[Quiz.Questions.Count]);
            while (UserAnswers.Count <= questionIndex) UserAnswers.Add(null!);
            UserAnswers[questionIndex] = selectedAnswer;

            HttpContext.Session.SetObject($"Quiz_{id}_Answers", UserAnswers);

            // Move to next question or results
            int nextIndex = questionIndex + 1;
            if (nextIndex >= Quiz.Questions.Count)
            {
                return RedirectToPage("/Results", new { id });
            }

            return RedirectToPage("/Quiz", new { id, questionIndex = nextIndex });
        }
    }
}