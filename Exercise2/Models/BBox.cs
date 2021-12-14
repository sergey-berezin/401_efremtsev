using System;
using System.Drawing;
using System.Linq;
using RecognizerModels.DataStructures;


namespace Exercise2.Models
{
    internal class BBox
    {
        public string Label { get; }
        public float Confidence { get; }
        public Rectangle Box { get; }

        public BBox(YoloV4Result predict)
        {
            Label = predict.Label;
            Confidence = predict.Confidence;

            var box = predict.BBox.Select(Convert.ToInt32).ToArray();
            Box = new Rectangle(box[0], box[1], box[2] - box[0], box[3] - box[1]);
        }
    }
}
