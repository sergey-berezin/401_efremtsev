using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApplication.Entity;

namespace WebApplication.Models
{
    public class ImagePost
    {
        [JsonPropertyName("imageName")]
        public string ImageName { get; set; }

        [JsonPropertyName("image")]
        public byte[] Image { get; set; }
    }


    public class ImageGet
    {
        public int Id { get; set; }
        public string ImageName { get; set; }
        public byte[] ImageData { get; set; }
        public int ImageHash { get; set; }
        public List<BBoxGet> BBoxes { get; set; }

        public ImageGet(ImageEntity entity)
        {
            Id = entity.Id;
            ImageName = entity.ImageName;
            ImageData = entity.ImageData;
            ImageHash = entity.ImageHash;
            BBoxes = new List<BBoxGet>();

            foreach (var entityBBox in entity.BBoxes)
            {
                BBoxes.Add(new BBoxGet(entityBBox));
            }

        }
    }

    public class BBoxGet
    {
        public string Label { get; set; }
        public float Confidence { get; set; }
        public int CoordX { get; set; }
        public int CoordY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public BBoxGet(BBoxEntity entity)
        {
            Label = entity.Label.Label;
            Confidence = entity.Confidence;
            CoordX = entity.CoordX;
            CoordY = entity.CoordY;
            Width = entity.Width;
            Height = entity.Height;
        }
    }

}
