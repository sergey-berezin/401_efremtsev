using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Exercise4.Commands.Base;
using Exercise4.Models;
using System.Text.Json;
using System.Threading;


namespace Exercise4.ViewModels
{
    internal class MainWindowViewModel: ViewModelBase
    {
        private HttpClient client;
        private CancellationTokenSource source;

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
            client = new HttpClient();

            RecognizedImage = new ObservableCollection<RecognizedImage>();
            ShowAllData();

            StartRecognitionCommand = new Command(StartRecognitionCommandExecute, StartRecognitionCommandCanExecute);
            CancelRecognitionCommand = new Command(CancelRecognitionCommandExecute, CancelRecognitionCommandCanExecute);
            RemoveDataFromDatabaseCommand = new Command(RemoveDataFromDatabaseCommandExecute, RemoveDataFromDatabaseCommandCanExecute);
        }

        //-------------------------------------------------------------------
        private async void ShowAllData()
        {
            try
            {
                var response = await client.GetAsync($"http://localhost:1173/api/Image");
                var images = await response.Content.ReadFromJsonAsync<MessageGet[]>();
                foreach (var image in images)
                {
                    RecognizedImage.Add(new RecognizedImage(image));
                }
            }
            catch
            {
                MessageBox.Show("Сервис недоступен.", "Внимание");
            }
        }

        private async Task StartRecognition()
        {
            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if (folderDialog.ShowDialog() ?? false)
            {
                RecognizedImage.Clear();
                source = new CancellationTokenSource();
                var folderPath = folderDialog.SelectedPaths[0];

                foreach (var imageFileInfo in new DirectoryInfo(folderPath).GetFiles())
                {
                    try
                    {
                        var imageBytes = await File.ReadAllBytesAsync(imageFileInfo.FullName);
                        var json = JsonSerializer.Serialize(new MessagePost()
                            {ImageName = imageFileInfo.Name, Image = imageBytes});
                        var message = new StringContent(json, Encoding.Default, "application/json");

                        var responsePost = await client.PostAsync("http://localhost:1173/api/Image", message, source.Token);
                        var id = await responsePost.Content.ReadFromJsonAsync<int>();
                        var responseGet = await client.GetAsync($"http://localhost:1173/api/Image/{id}", source.Token);
                        var image = await responseGet.Content.ReadFromJsonAsync<MessageGet>();
                        RecognizedImage.Add(new RecognizedImage(image));
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        MessageBox.Show("Сервис недоступен.", "Внимание");
                        return;
                    }
                }

                MessageBox.Show("Распознование окончено.", "Внимание");
            }
        }

        private void CancelRecognition()
        {
            source.Cancel();
        }

        private async Task RemoveData()
        {
            if (SelectedImage != null)
            {
                try
                {
                    var a = await client.DeleteAsync($"http://localhost:1173/api/Image/{SelectedImage.Id}");
                }
                catch
                {
                    MessageBox.Show("Сервис недоступен.", "Внимание");
                    return;
                }

                RecognizedImage.Remove(SelectedImage);

            }
        }
    }
}
