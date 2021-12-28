using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecognizerModels;
using WebApplication;
using WebApplication.Converter;
using WebApplication.Entity;
using WebApplication.Models;

namespace WebApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ContextDb _context;

        public ImageController(ContextDb context)
        {
            _context = context;
        }

        // GET: api/Image
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ImageGet>>> GetImages()
        {
            return (from imageEntity in await _context.Images.ToListAsync() select new ImageGet(imageEntity)).ToList();
        }

        // GET: api/Image/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ImageGet>> GetImageEntity(int id)
        {
            var image = await _context.Images.FindAsync(id);

            if (image == null)
            {
                return NotFound();
            }

            var recognizer = new Recognizer();

            await foreach (var predict in recognizer.StartRecognition(image.ImageData))
            {
                var label = await _context.Labels.FirstOrDefaultAsync(obj => obj.Label == predict.Label);

                var inDb = true;

                if (label == null)
                {
                    label = new LabelEntity() { Label = predict.Label };
                    await _context.Labels.AddAsync(label);
                    await _context.SaveChangesAsync();
                    inDb = false;
                }

                var newBBox = Converters.YoloV4ResultToBBoxEntity(predict, label, image);

                if (inDb)
                {
                    var bboxes = await _context.BBoxes
                        .Where(obj => obj.Label == label && obj.Image == image)
                        .ToArrayAsync();

                    inDb = bboxes.Any(bbox => bbox.Equals(newBBox));
                }

                if (!inDb)
                {
                    await _context.BBoxes.AddAsync(newBBox);
                    await _context.SaveChangesAsync();
                }
            }

            return new ImageGet(image);
        }

        // POST: api/Image
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<int>> PostImageEntity([FromBody] ImagePost imageObj)
        {
            var imageName = imageObj.ImageName;
            var image = imageObj.Image;

            var imageEntity = new ImageEntity()
            {
                ImageName = imageName, 
                ImageData = image, 
                ImageHash = Converters.GetHashFromBytes(image)
            };

            var images = await _context.Images
                .Where(obj => obj.ImageHash == Converters.GetHashFromBytes(imageEntity.ImageData))
                .ToArrayAsync()
                .ConfigureAwait(false);

            foreach (var imageDb in images)
            {
                if (imageDb.ImageData.SequenceEqual(imageEntity.ImageData))
                {
                    return Ok(imageDb.Id);
                }
            }

            await _context.Images.AddAsync(imageEntity);
            await _context.SaveChangesAsync();

            return Ok(imageEntity.Id);
        }

        // DELETE: api/Image/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImageEntity(int id)
        {
            var imageEntity = await _context.Images.FindAsync(id);

            if (imageEntity == null)
            {
                return NotFound();
            }

            foreach (var bbox in imageEntity.BBoxes)
            {
                _context.BBoxes.Remove(bbox);
            }

            _context.Images.Remove(imageEntity);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
