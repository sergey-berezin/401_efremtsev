using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using Microsoft.ML;
using Microsoft.ML.Transforms.Onnx;
using Microsoft.ML.Data;
using RecognizerModels.DataStructures;
using System.Threading.Tasks.Dataflow;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;


namespace RecognizerModels
{
    public class Recognizer
    {
        private static readonly string modelPath = @"C:\Users\User\Model\yolov4.onnx";

        static readonly string[] classesNames = {
            "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", 
            "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", 
            "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", 
            "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", 
            "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", 
            "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", 
            "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", 
            "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", 
            "scissors", "teddy bear", "hair drier", "toothbrush"
        };

        private TransformBlock<Bitmap, YoloV4Prediction>? _prediction;
        private TransformManyBlock<YoloV4Prediction, YoloV4Result>? _getResult;
        private BufferBlock<YoloV4Result>? _resultBuffer;
        private CancellationTokenSource? _cancellationTokenSource;

        private MLContext _mlContext;
        private TransformerChain<OnnxTransformer> _model;

        public Recognizer()
        {
            InitModel(modelPath);
        }

        private void InitModel(string modelPath)
        {
            _mlContext = new MLContext();

            var pipeline = _mlContext.Transforms.ResizeImages(
                    inputColumnName: "bitmap", 
                    outputColumnName: "input_1:0",
                    imageWidth: 416, 
                    imageHeight: 416, 
                    resizing: ResizingKind.IsoPad)
                .Append(_mlContext.Transforms.ExtractPixels(
                    outputColumnName: "input_1:0", 
                    scaleImage: 1f / 255f, 
                    interleavePixelColors: true))
                .Append(_mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                        { "input_1:0", new[] { 1, 416, 416, 3 } },
                        { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                        { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                        { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                        "input_1:0"
                    },
                    outputColumnNames: new[]
                    {
                        "Identity:0",
                        "Identity_1:0",
                        "Identity_2:0"
                    },
                    modelFile: modelPath, 
                    recursionLimit: 100));

            _model = pipeline.Fit(_mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

        }

        private void InitDataflow()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _prediction = new TransformBlock<Bitmap, YoloV4Prediction>(
                image =>
                {
                    var predictionEngine = _mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(_model);

                    return predictionEngine.Predict(new YoloV4BitmapData() {Image = image});
                },
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = _cancellationTokenSource.Token,
                    MaxDegreeOfParallelism = 4
                });

            _getResult = new TransformManyBlock<YoloV4Prediction, YoloV4Result>(
                predict => predict.GetResults(classesNames, 0.3f, 0.7f),
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = _cancellationTokenSource.Token,
                    MaxDegreeOfParallelism = 2
                }
            );

            _resultBuffer = new BufferBlock<YoloV4Result>(
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = _cancellationTokenSource.Token
                }
            );
            ;
            _prediction.LinkTo(_getResult, new DataflowLinkOptions { PropagateCompletion = true });
            _getResult.LinkTo(_resultBuffer, new DataflowLinkOptions { PropagateCompletion = true });
        }


        public async IAsyncEnumerable<YoloV4Result> StartRecognition(byte[] image)
        {
            InitDataflow();

            Image img;

            using (MemoryStream memstr = new MemoryStream(image))
            {
                img = Image.FromStream(memstr);
            }

            _prediction.Post(new Bitmap(img));

            _prediction.Complete();

            await foreach (var result in _resultBuffer.ReceiveAllAsync())
            {
                yield return result;
            }
        }

        public void CancelRecognition() => _cancellationTokenSource?.Cancel();
    }
}
