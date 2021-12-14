using RecognizerModels;
using System;
using System.Threading.Tasks;


namespace Exercise1
{
    class Program
    {
        private static string modelPath = @"Model\yolov4.onnx";

        static async Task Main(string[] args)
        {
            Recognizer recognizer = new Recognizer(modelPath);

            Console.WriteLine("Введите путь к каталогу с изображениями.");
            string pathFolder = Console.ReadLine();

            Task cancelTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        recognizer.CancelRecognition();
                        Console.WriteLine("Распознование было остановлено пользователем.");
                        break;
                    }
                }
            }, creationOptions: TaskCreationOptions.LongRunning);

            Console.WriteLine("Нажмите клавишу ESC, чтобы остановить распознование.");

            await foreach (var (imagePath, predict) in recognizer.StartRecognition(pathFolder))
            {
                Console.WriteLine($"{imagePath}: {predict.Label}");
            }
        }
    }
}
