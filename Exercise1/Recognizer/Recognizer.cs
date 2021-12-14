using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
using RecognizerModels.DataStructures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;


namespace RecognizerModels
{
    public class Recognizer
    {
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

        private TransformBlock<string, Tuple<string, Bitmap>>? _loadImage;
        private TransformBlock<Tuple<string, Bitmap>, Tuple<string, YoloV4Prediction>>? _prediction;
        private TransformManyBlock<Tuple<string, YoloV4Prediction>, Tuple<string, YoloV4Result>>? _getResult;
        private BufferBlock<Tuple<string, YoloV4Result>>? _resultBuffer;
        private CancellationTokenSource? _cancellationTokenSource;

        private MLContext _mlContext;
        private TransformerChain<OnnxTransformer> _model;

        public Recognizer(string modelPath)
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

            _loadImage = new TransformBlock<string, Tuple<string, Bitmap>>(
                imagePath =>
                {
                    try
                    {
                        Bitmap image = new Bitmap(imagePath);
                        return new Tuple<string, Bitmap>(imagePath, image);
                    }
                    catch
                    {
                        return null;
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = _cancellationTokenSource.Token,
                    MaxDegreeOfParallelism = 1
                });

            _prediction = new TransformBlock<Tuple<string, Bitmap>, Tuple<string, YoloV4Prediction>>(
                imageTuple =>
                {
                    var (imagePath, image) = imageTuple;
                    var predictionEngine = _mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(_model);

                    return new Tuple<string, YoloV4Prediction>(imagePath, predictionEngine.Predict(new YoloV4BitmapData() { Image = image }));
                },
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = _cancellationTokenSource.Token,
                    MaxDegreeOfParallelism = 4
                });

            _getResult = new TransformManyBlock<Tuple<string, YoloV4Prediction>, Tuple<string, YoloV4Result>>(
                GetResults,
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = _cancellationTokenSource.Token,
                    MaxDegreeOfParallelism = 2
                }
            );

            _resultBuffer = new BufferBlock<Tuple<string, YoloV4Result>>(
                new ExecutionDataflowBlockOptions
                {
                    CancellationToken = _cancellationTokenSource.Token
                }
            );

            _loadImage.LinkTo(_prediction, new DataflowLinkOptions { PropagateCompletion = true }, image => image != null);
            _prediction.LinkTo(_getResult, new DataflowLinkOptions { PropagateCompletion = true });
            _getResult.LinkTo(_resultBuffer, new DataflowLinkOptions { PropagateCompletion = true });
        }

        private IEnumerable<Tuple<string, YoloV4Result>> GetResults(Tuple<string, YoloV4Prediction> predictTuple)
        {
            var (imagePath, predict) = predictTuple;

            foreach (var result in predict.GetResults(classesNames, 0.3f, 0.7f))
            {
                yield return new Tuple<string, YoloV4Result>(imagePath, result);
            }
        }

        public async IAsyncEnumerable<Tuple<string, YoloV4Result>> StartRecognition(string pathToFolder)
        {
            InitDataflow();

            string[] filesPath = Directory.GetFiles(pathToFolder);

            foreach (var filePath in filesPath)
            {
                _loadImage.Post(filePath);
            }

            _loadImage.Complete();

            await foreach (var result in _resultBuffer.ReceiveAllAsync())
            {
                yield return result;
            }
        }

        public void CancelRecognition() => _cancellationTokenSource.Cancel();
    }
}
