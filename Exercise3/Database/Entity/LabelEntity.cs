using System;
using System.Collections.Generic;
using System.Text;


namespace Database.Entity
{
    public class LabelEntity
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public virtual ICollection<BBoxEntity> BBoxes { get; set; }
    }
}
