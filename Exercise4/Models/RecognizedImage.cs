using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;
using Exercise4.Models;


namespace Exercise4.Models
{
    public class RecognizedImage
    {
        public int Id { get; set; }
        public string ImageName { get; }
        public byte[] ImageBytes { get; }
        public int Hash { get; set; }
        public ObservableCollection<BBox> BBoxes { get; }

        public RecognizedImage(MessageGet image)
        {
            Id = image.Id;
            ImageName = image.ImageName;
            ImageBytes = image.ImageData;
            Hash = image.ImageHash;
            BBoxes = new ObservableCollection<BBox>();

            foreach (var bbox in image.BBoxes)
            {
                BBoxes.Add(new BBox(bbox));
            }
        }
    }
}
