using System;
using System.Drawing;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace WebApplication.Entity
{
    public class BBoxEntity
    {
        public int Id { get; set; }
        public virtual LabelEntity Label { get; set; }
        public virtual ImageEntity Image { get; set; }

        public float Confidence { get; set; }
        public int CoordX { get; set; }
        public int CoordY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as BBoxEntity);
        }

        public bool Equals(BBoxEntity other)
        {
            return other != null &&
                   Math.Abs(Confidence - other.Confidence) < 1e-12 &&
                   CoordX == other.CoordX &&
                   CoordY == other.CoordY &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Confidence, CoordX, CoordY, Width, Height);
        }
    }
}
