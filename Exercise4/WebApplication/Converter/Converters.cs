using System;
using System.Linq;
using System.Numerics;
using RecognizerModels.DataStructures;
using WebApplication.Entity;


namespace WebApplication.Converter
{
    public static class Converters
    {
        public static BBoxEntity YoloV4ResultToBBoxEntity(YoloV4Result predict, LabelEntity label, ImageEntity image)
        {
            var confidence = predict.Confidence;
            var box = predict.BBox.Select(Convert.ToInt32).ToArray();

            return new BBoxEntity
            {
                Label = label,
                Image = image,
                Confidence = confidence,
                CoordX = box[0],
                CoordY = box[1],
                Width = box[2] - box[0],
                Height = box[3] - box[1]
            };
        }

        public static int GetHashFromBytes(byte[] bytes)
        {
            return new BigInteger(bytes).GetHashCode();
        }

    }
}
