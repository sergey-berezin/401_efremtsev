using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Exercise3.Commands.Base;
using Exercise3.Models;
using Exercise3.Utilities;
using RecognizerModels;


namespace Exercise3.ViewModels
{
    internal class MainWindowViewModel: ViewModelBase
    {
        private readonly Recognizer _recognizer;
        private readonly DatabaseWorker _database;

        //-------------------------------------------------------------------
        private bool _IsRecognition = true;
        public bool IsRecognition
        {
            get => _IsRecognition;
            set => Set(ref _IsRecognition, value);
        }

        private bool _IsRemove = true;
        public bool IsRemove
        {
            get => _IsRecognition;
            set => Set(ref _IsRemove, value);
        }

        //-------------------------------------------------------------------
        public ObservableCollection<RecognizedImage> RecognizedImage { get; }

        private RecognizedImage _SelectedImage;
        public RecognizedImage SelectedImage
        {
            get => _SelectedImage;
            set => Set(ref _SelectedImage, value);
        }

        //-------------------------------------------------------------------
        public Command StartRecognitionCommand { get; }
        private async void StartRecognitionCommandExecute(object _)
        {
            IsRecognition = false;
            await StartRecognition();
            IsRecognition = true;
        }
        private bool StartRecognitionCommandCanExecute(object _)
        {
            return IsRecognition && IsRemove;
        }

        public Command CancelRecognitionCommand { get; }
        private void CancelRecognitionCommandExecute(object _)
        {
            CancelRecognition();
            IsRecognition = true;
        }
        private bool CancelRecognitionCommandCanExecute(object _)
        {
            return !IsRecognition;
        }

        public Command RemoveDataFromDatabaseCommand { get; }
        private async void RemoveDataFromDatabaseCommandExecute(object _)
        {
            IsRemove = false;
            await RemoveData();
            IsRemove = true;
        }
        private bool RemoveDataFromDatabaseCommandCanExecute(object _)
        {
            return IsRecognition && IsRemove;
        }


        //-------------------------------------------------------------------
        public MainWindowViewModel()
        {
            _recognizer = new Recognizer();
            _database = new DatabaseWorker();

            RecognizedImage = new ObservableCollection<RecognizedImage>();
            ShowAllData();

            StartRecognitionCommand = new Command(StartRecognitionCommandExecute, StartRecognitionCommandCanExecute);
            CancelRecognitionCommand = new Command(CancelRecognitionCommandExecute, CancelRecognitionCommandCanExecute);
            RemoveDataFromDatabaseCommand = new Command(RemoveDataFromDatabaseCommandExecute, RemoveDataFromDatabaseCommandCanExecute);
        }

        //-------------------------------------------------------------------
        private void ShowAllData()
        {
            foreach (var image in _database.GetAllData())
            {
                RecognizedImage.Add(new RecognizedImage(image.ImageName, image.ImageData));
                foreach (var bbox in image.BBoxes)
                {
                    RecognizedImage.Last().Add(bbox);
                }
            }
        }

        private async Task StartRecognition()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if (folderDialog.ShowDialog() ?? false)
            {
                RecognizedImage.Clear();
                var folderPath = folderDialog.SelectedPaths[0];
                var image2id = new Dictionary<string, int>();

                int maxId = 0;
                foreach (var imageFileInfo in new DirectoryInfo(folderPath).GetFiles())
                {
                    var imageBytes = await File.ReadAllBytesAsync(imageFileInfo.FullName);
                    RecognizedImage.Add(new RecognizedImage(imageFileInfo.Name, imageBytes));
                    image2id[imageFileInfo.FullName] = maxId;
                    ++maxId;
                }

                await foreach (var (imagePath, predict) in _recognizer.StartRecognition(folderPath))
                {
                    RecognizedImage[image2id[imagePath]].Add(predict);
                    await _database.Add(predict, RecognizedImage[image2id[imagePath]].ImageBytes, RecognizedImage[image2id[imagePath]].ImageName);
                }

                MessageBox.Show("Распознование окончено.", "Внимание");
            }
        }

        private void CancelRecognition()
        {
            _recognizer.CancelRecognition();
        }

        private async Task RemoveData()
        {
            await _database.Remove(_SelectedImage);
            RecognizedImage.Remove(_SelectedImage);
        }
    }
}
