using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using RecognizerModels.DataStructures;

namespace Exercise2.Models
{
    internal class RecognizedImage
    {
        public string ImageName { get; }
        public string ImagePath { get; }
        public ObservableCollection<BBox> BBoxes { get; }

        public RecognizedImage(string imageName, string imagePath)
        {
            ImageName = imageName;
            ImagePath = imagePath;
            BBoxes = new ObservableCollection<BBox>();
        }

        public void Add(YoloV4Result predict) => BBoxes.Add(new BBox(predict));
    }
}
