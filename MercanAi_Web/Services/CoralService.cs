using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MercanAi_Web.Services
{
    public class PredictionResult
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
        public string ModelUsed { get; set; }
        public float RawScore { get; set; }
    }

    public class CoralService
    {
        private readonly string _modelFolderPath;

        public CoralService(IWebHostEnvironment env)
        {
            _modelFolderPath = Path.Combine(env.ContentRootPath, "AI_Models", "modeller_final");
        }

        public PredictionResult PredictImage(Stream imageStream, string modelName)
        {
            string modelPath = Path.Combine(_modelFolderPath, modelName + ".onnx");

            var tensor = ProcessImage(imageStream, modelName);

            using var session = new InferenceSession(modelPath);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_image", tensor)
            };

            using var results = session.Run(inputs);

            float rawScore = results.First().AsTensor<float>().ToArray()[0];
            bool isHealthy = rawScore >= 0.5f;

            float confidence = isHealthy
                ? rawScore * 100f
                : (1f - rawScore) * 100f;

            return new PredictionResult
            {
                ModelUsed = modelName,
                Label = isHealthy ? "SAĞLIKLI MERCAN" : "BEYAZLAMIŞ MERCAN",
                Confidence = confidence,
                RawScore = rawScore
            };
        }

        private DenseTensor<float> ProcessImage(Stream imageStream, string modelName)
        {
            if (imageStream.CanSeek) imageStream.Position = 0;

            using var image = Image.Load<Rgb24>(imageStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(224, 224),
                Mode = ResizeMode.Stretch
            }));

            var tensor = new DenseTensor<float>(new[] { 1, 224, 224, 3 });

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        var p = row[x];

                        if (modelName == "MobileNetV2")
                        {
                            tensor[0, y, x, 0] = (p.R / 127.5f) - 1f;
                            tensor[0, y, x, 1] = (p.G / 127.5f) - 1f;
                            tensor[0, y, x, 2] = (p.B / 127.5f) - 1f;
                        }
                        else if (modelName == "ResNet50")
                        {
                            tensor[0, y, x, 0] = p.B - 103.939f;
                            tensor[0, y, x, 1] = p.G - 116.779f;
                            tensor[0, y, x, 2] = p.R - 123.68f;
                        }
                        else // EfficientNetB0
                        {
                            tensor[0, y, x, 0] = p.R / 255f;
                            tensor[0, y, x, 1] = p.G / 255f;
                            tensor[0, y, x, 2] = p.B / 255f;

                        }
                    }
                }
            });

            return tensor;
        }
    }
}
