using Microsoft.AspNetCore.Mvc;
using MercanAi_Web.Models;
using MercanAi_Web.Services;
using System.Text.Json;

namespace MercanAI_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly CoralService _coralService;
        private readonly IWebHostEnvironment _env;

        public HomeController(CoralService coralService, IWebHostEnvironment env)
        {
            _coralService = coralService;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new DashboardViewModel();
            model.Metrics = LoadMetricsFromJson();
            return View(model);
        }

        [HttpPost]
        public IActionResult Analyze(IFormFile file, string selectedModel)
        {
            var viewModel = new DashboardViewModel();
            viewModel.Metrics = LoadMetricsFromJson(); 

            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Lütfen bir resim seçiniz.";
                return View("Index", viewModel);
            }

            try
            {
                
                var result = _coralService.PredictImage(file.OpenReadStream(), selectedModel);

                viewModel.ResultLabel = result.Label;
                viewModel.ResultConfidence = result.Confidence;
                viewModel.UsedModel = result.ModelUsed;

                
                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    viewModel.UploadedImageBase64 = Convert.ToBase64String(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Hata: " + ex.Message;
            }

            return View("Index", viewModel);
        }

        private List<ModelMetric> LoadMetricsFromJson()
        {
            try
            {
                string jsonPath = Path.Combine(_env.ContentRootPath, "AI_Models", "model_metrics.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    string jsonString = System.IO.File.ReadAllText(jsonPath);
                    return JsonSerializer.Deserialize<List<ModelMetric>>(jsonString);
                }
            }
            catch { }
            return new List<ModelMetric>();
        }
    }
}