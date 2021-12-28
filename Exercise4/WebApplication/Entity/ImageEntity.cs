using System;
using System.Collections.Generic;
using System.Text;


namespace WebApplication.Entity
{
    public class ImageEntity
    {
        public int Id { get; set; }
        public string ImageName { get; set; }
        public virtual byte[] ImageData { get; set; }
        public int ImageHash { get; set; }
        public virtual ICollection<BBoxEntity> BBoxes { get; set; }
    }
}
