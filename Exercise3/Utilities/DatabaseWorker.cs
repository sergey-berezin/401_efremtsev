using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Entity;
using Exercise3.Converter;
using Exercise3.Models;
using Microsoft.EntityFrameworkCore;
using RecognizerModels.DataStructures;

namespace Exercise3.Utilities
{
    public class DatabaseWorker
    {
        private readonly ContextDb _context;

        public DatabaseWorker()
        {
            _context = new ContextDb();
        }

        public IEnumerable<ImageEntity> GetAllData()
        {
            foreach (var image in _context.Images.ToArray())
            {
                yield return image;
            }
        }

        public async Task Add(YoloV4Result predict, byte[] imageBytes, string imageName)
        {
            bool inDb = true;

            var label = await _context.Labels
                .FirstOrDefaultAsync(obj => obj.Label == predict.Label)
                .ConfigureAwait(false);

            if (label == null)
            {
                label = new LabelEntity() { Label = predict.Label };
                _context.Entry(label).State = EntityState.Added;
                await _context.SaveChangesAsync().ConfigureAwait(false);
                inDb = false;
            }

            var image = await _context.Images
                .FirstOrDefaultAsync(obj => obj.ImageHash == Converters.GetHashFromBytes(imageBytes))
                .ConfigureAwait(false);

            if (image == null)
            {
                image = new ImageEntity() { ImageName = imageName, ImageData = imageBytes, ImageHash = Converters.GetHashFromBytes(imageBytes) };
                _context.Entry(image).State = EntityState.Added;
                await _context.SaveChangesAsync().ConfigureAwait(false);
                inDb = false;
            }

            var newBBox = Converters.YoloV4ResultToBBoxEntity(predict, label, image);

            if (inDb)
            {
                var bboxes = await _context.BBoxes
                    .Where(obj => obj.Label == label && obj.Image == image)
                    .ToArrayAsync()
                    .ConfigureAwait(false);

                inDb = bboxes.Any(bbox => bbox.Equals(newBBox));
            }

            if (!inDb)
            {
                _context.Entry(newBBox).State = EntityState.Added;
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }

        }

        public async Task Remove(RecognizedImage selectedImage)
        {
            var images = await _context.Images
                .Where(obj => obj.ImageHash == Converters.GetHashFromBytes(selectedImage.ImageBytes))
                .ToArrayAsync()
                .ConfigureAwait(false);

            foreach (var image in images)
            {
                if (image.ImageData.SequenceEqual(selectedImage.ImageBytes))
                {
                    _context.Remove(image);
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                    break;
                }
            }
        }
    }
}
