using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Exercise4.Models
{
    public class MessagePost
    {
        [JsonPropertyName("imageName")] public string ImageName { get; set; }
        [JsonPropertyName("image")] public byte[] Image { get; set; }
    }

    public class MessageGet
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("imageName")] public string ImageName { get; set; }
        [JsonPropertyName("imageData")] public byte[] ImageData { get; set; }
        [JsonPropertyName("imageHash")] public int ImageHash { get; set; }
        [JsonPropertyName("bBoxes")] public List<BBoxGet> BBoxes { get; set; }
    }

    public class BBoxGet
    {
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("confidence")] public float Confidence { get; set; }
        [JsonPropertyName("coordX")] public int CoordX { get; set; }
        [JsonPropertyName("coordY")] public int CoordY { get; set; }
        [JsonPropertyName("width")] public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
    }
}
