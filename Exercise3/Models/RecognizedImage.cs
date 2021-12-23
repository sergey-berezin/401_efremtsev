using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using Database.Entity;
using RecognizerModels.DataStructures;
using Exercise3.Models;


namespace Exercise3.Models
{
    public class RecognizedImage
    {
        public string ImageName { get; }
        public byte[] ImageBytes { get; }
        public ObservableCollection<BBox> BBoxes { get; }

        public RecognizedImage(string imageName, byte[] imageBytes)
        {
            ImageName = imageName;
            ImageBytes = imageBytes;
            BBoxes = new ObservableCollection<BBox>();
        }

        public void Add(YoloV4Result predict) => BBoxes.Add(new BBox(predict));
        public void Add(BBoxEntity bbox) => BBoxes.Add(new BBox(bbox));
    }
}
