using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using patoquiz.Data;
using patoquiz.Models;
using System.ComponentModel.DataAnnotations;

namespace patoquiz.Pages
{
    public class AdminModel : PageModel
    {
        private readonly QuizDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminModel(QuizDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public List<Subspecialty> Subspecialties { get; set; } = new();
        public List<Quiz> Quizzes { get; set; } = new();
        public Quiz? EditingQuiz { get; set; }
        public QuestionInput? PendingQuestion { get; set; }
        public AnswerInput? PendingAnswer { get; set; }
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public LoginInput Login { get; set; } = new();
        [BindProperty]
        public SubspecialtyInput NewSubspecialty { get; set; } = new();
        [BindProperty]
        public QuizInput NewQuiz { get; set; } = new();

        public class LoginInput
        {
            [Required]
            public string Username { get; set; } = string.Empty;
            [Required]
            public string Password { get; set; } = string.Empty;
        }

        public class SubspecialtyInput
        {
            [Required]
            public string Name { get; set; } = string.Empty;
        }

        public class QuizInput
        {
            [Required]
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            [Required]
            public int SubspecialtyId { get; set; }
        }

        public class QuestionInput
        {
            [Required]
            public string Text { get; set; } = string.Empty;
            public IFormFile? ImageUpload { get; set; }
            public int QuizId { get; set; }
            public List<AnswerInput> Answers { get; set; } = new();
            public bool IsCopy { get; set; }
        }

        public class AnswerInput
        {
            [Required]
            public string Text { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
            public string? Explanation { get; set; }
            public int QuestionId { get; set; }
        }

        public IActionResult OnGet(int? editQuizId )
        {
            if (!IsAdmin()) return Page();
            LoadData();
            if (editQuizId.HasValue)
            {
                EditingQuiz = Quizzes.FirstOrDefault(q => q.Id == editQuizId);
            }
            return Page();
        }

        public IActionResult OnPostLogin()
        {
            if (Login.Username == "admin" && Login.Password == "quizmaster!JHB")
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                LoadData();
                return Page();
            }
            Message = "Invalid credentials.";
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Admin");
        }

        public IActionResult OnPostCreateSubspecialty()
        {
            if (!IsAdmin()) return RedirectToPage("/Admin");
            var subspecialty = new Subspecialty { Name = NewSubspecialty.Name };
            _context.Subspecialties.Add(subspecialty);
            _context.SaveChanges();
            Message = "Subspecialty created.";
            LoadData();
            return Page();
        }

        public IActionResult OnPostCreateQuiz()
        {
            if (!IsAdmin()) return RedirectToPage("/Admin");
            var quiz = new Quiz { Title = NewQuiz.Title, Description = NewQuiz.Description, SubspecialtyId = NewQuiz.SubspecialtyId };
            _context.Quizzes.Add(quiz);
            _context.SaveChanges();
            Message = "Quiz created. Editing now.";
            EditingQuiz = quiz;
            LoadData();
            return Page();
        }

       

        public IActionResult OnPostCopyQuestion(int quizId, int copyQuestionId)
        {
            if (!IsAdmin()) return RedirectToPage("/Admin");
            if (PendingQuestion != null)
            {
                Message = "Finish the current question first.";
            }
            else
            {
                var original = _context.Questions.Include(q => q.Answers).FirstOrDefault(q => q.Id == copyQuestionId);
                if (original != null)
                {
                    PendingQuestion = new QuestionInput
                    {
                        Text = original.Text,
                        QuizId = quizId,
                        Answers = original.Answers.Select(a => new AnswerInput
                        {
                            Text = a.Text,
                            Explanation = a.Explanation,
                            IsCorrect = false
                        }).ToList(),
                        IsCopy = true
                    };
                }
            }
            EditingQuiz = _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefault(q => q.Id == quizId);
            LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveCopiedQuestion(int quizId)
        {
            if (!IsAdmin() || PendingQuestion == null) return RedirectToPage("/Admin");
            var imagePath = PendingQuestion.ImageUpload != null ? await SaveImage(PendingQuestion.ImageUpload) : "/images/default.png";
            var question = new Question
            {
                Text = PendingQuestion.Text,
                ImagePath = imagePath,
                QuizId = quizId,
                Answers = PendingQuestion.Answers.Select(a => new Answer
                {
                    Text = a.Text,
                    IsCorrect = a.IsCorrect,
                    Explanation = a.Explanation ?? string.Empty
                }).ToList()
            };
            _context.Questions.Add(question);
            _context.SaveChanges();
            PendingQuestion = null;
            Message = "Copied question saved.";
            EditingQuiz = _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefault(q => q.Id == quizId);
            LoadData();
            return Page();
        }

        public IActionResult OnPostAddQuestion(int quizId)
        {
            if (!IsAdmin()) return RedirectToPage("/Admin");
            if (PendingQuestion != null)
            {
                Message = "Finish the current question first.";
            }
            else
            {
                PendingQuestion = new QuestionInput { QuizId = quizId };
                Console.WriteLine($"Added PendingQuestion: QuizId={quizId}");
            }
            EditingQuiz = _context.Quizzes
                .Include(q => q.Questions!)
                .ThenInclude(q => q.Answers!)
                .FirstOrDefault(q => q.Id == quizId);
            LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveQuestion(int quizId)
        {
            if (!IsAdmin())
            {
                Message = "Admin access required.";
                return Page();
            }

            // Forceful logging
            Console.WriteLine($"Entering OnPostSaveQuestion: quizId={quizId}, PendingQuestion={(PendingQuestion != null ? $"Text={PendingQuestion.Text}, Image={PendingQuestion.ImageUpload?.FileName}" : "null")}");

            if (PendingQuestion == null)
            {
                Message = "No question to save.";
                EditingQuiz = _context.Quizzes
                    .Include(q => q.Questions!)
                    .ThenInclude(q => q.Answers!)
                    .FirstOrDefault(q => q.Id == quizId);
                LoadData();
                Console.WriteLine("PendingQuestion was null, exiting.");
                return Page();
            }

            await TryUpdateModelAsync(PendingQuestion, "PendingQuestion", q => q.Text, q => q.ImageUpload);

            Console.WriteLine($"After binding: Text={PendingQuestion.Text}, Image={PendingQuestion.ImageUpload?.FileName}");

            if (!ModelState.IsValid)
            {
                Message = "Invalid question: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                EditingQuiz = _context.Quizzes
                    .Include(q => q.Questions!)
                    .ThenInclude(q => q.Answers!)
                    .FirstOrDefault(q => q.Id == quizId);
                LoadData();
                Console.WriteLine("ModelState invalid, exiting.");
                return Page();
            }

            var imagePath = PendingQuestion.ImageUpload != null ? await SaveImage(PendingQuestion.ImageUpload) : "/images/default.png";
            var question = new Question { Text = PendingQuestion.Text, ImagePath = imagePath, QuizId = quizId };
            _context.Questions.Add(question);
            _context.SaveChanges();
            PendingQuestion = null;
            Message = "Question added.";
            EditingQuiz = _context.Quizzes
                .Include(q => q.Questions!)
                .ThenInclude(q => q.Answers!)
                .FirstOrDefault(q => q.Id == quizId);
            LoadData();
            Console.WriteLine("Question saved successfully.");
            return Page();
        }

        public IActionResult OnPostDelete(int id, string type)
        {
            if (!IsAdmin()) return RedirectToPage("/Admin");
            switch (type)
            {
                case "Subspecialty":
                    var subspecialty = _context.Subspecialties.Find(id);
                    if (subspecialty != null) _context.Subspecialties.Remove(subspecialty);
                    break;
                case "Quiz":
                    var quiz = _context.Quizzes.Find(id);
                    if (quiz != null) _context.Quizzes.Remove(quiz);
                    break;
                case "Question":
                    var question = _context.Questions.Find(id);
                    if (question != null) _context.Questions.Remove(question);
                    break;
                case "Answer":
                    var answer = _context.Answers.Find(id);
                    if (answer != null) _context.Answers.Remove(answer);
                    break;
            }
            _context.SaveChanges();
            Message = $"{type} deleted.";
            LoadData();
            return Page();
        }

        public bool IsAdmin() => HttpContext.Session.GetString("IsAdmin") == "true";

        private void LoadData()
        {
            Subspecialties = _context.Subspecialties.ToList();
            Quizzes = _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .Include(q => q.Subspecialty)
                .ToList();
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var path = Path.Combine(_env.WebRootPath, "images", fileName);
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }
            return "/images/" + fileName;
        }
    }
}