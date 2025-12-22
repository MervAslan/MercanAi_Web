namespace MercanAi_Web.Models
{
    public class DashboardViewModel
    {
        // Ekrana basılacak sonuçlar
        public string ResultLabel { get; set; }      
        public float ResultConfidence { get; set; }  
        public string UploadedImageBase64 { get; set; } 
        public string UsedModel { get; set; }

        // JSON'dan gelecek istatistikler
        public List<ModelMetric> Metrics { get; set; } = new List<ModelMetric>();
    }

    public class ModelMetric
    {
        public string ModelName { get; set; }
        public float TestAccuracy { get; set; }
        public float TestLoss { get; set; }
        public float AUC { get; set; }
      
    }
}