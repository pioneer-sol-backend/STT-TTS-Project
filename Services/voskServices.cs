using Vosk;

namespace ST_TTS_Project.Services
{
    public class VoskService
    {
        private readonly Model _model;

        public VoskService(IWebHostEnvironment environment)
        {
            var modelPath = Path.Combine(
                environment.ContentRootPath,
                "VoskModels",
                "vosk-model-small-en-us-0.15");

            _model = new Model(modelPath);
        }

        public VoskRecognizer CreateRecognizer()
        {
            return new VoskRecognizer(_model, 16000.0f);
        }
    }
}