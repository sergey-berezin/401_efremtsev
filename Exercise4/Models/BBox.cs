using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Exercise4.Models
{
    public class BBox
    {
        public string Label { get; }
        public float Confidence { get; }
        public Rectangle Box { get; }

        public BBox(BBoxGet bbox)
        {
            Label = bbox.Label;
            Confidence = bbox.Confidence;
            Box = new Rectangle(bbox.CoordX, bbox.CoordY, bbox.Width, bbox.Height);
        }
    }
}
