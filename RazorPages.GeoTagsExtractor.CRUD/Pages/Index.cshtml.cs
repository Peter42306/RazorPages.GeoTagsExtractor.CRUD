using MetadataExtractor.Formats.Exif;
using MetadataExtractor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RazorPages.GeoTagsExtractor.CRUD.Models;
using System.Drawing.Imaging;
using System.Drawing;

namespace RazorPages.GeoTagsExtractor.CRUD.Pages
{
    public class IndexModel : PageModel
    {

		public IEnumerable<FileModel> FilesCollection { get; set; } = default!; // свойство для хранения коллекции загруженных файлов, которые извлекаются из базы данных

		private readonly ApplicationContext _context;
		private readonly IWebHostEnvironment _environment;

        public IndexModel(ApplicationContext context, IWebHostEnvironment environment)
        {
			_context = context;
			_environment = environment;
		}

		/// <summary>
		/// Метод для получение списка всех файлов из базы данных и передачи их в представление страницы для отображения
		/// </summary>
		/// <returns></returns>
		public async Task OnGetAsync()
		{
			FilesCollection=await _context.Files.ToListAsync();
		}

		[BindProperty]
		public IFormFile UploadedFile { get; set; } = default!; // свойство для привязки загружаемого файла из формы на странице


		public async Task<IActionResult> OnPostAsync()
		{
			if (UploadedFile != null)
			{
				// Задаем путь к файлу
				string path = Path.Combine("Files", UploadedFile.FileName);
				string fullPath = Path.Combine(_environment.WebRootPath, path);

				GeoTags geoTags = null; // Объявляем переменную geoTags (до блока using)

				// Создаем временный поток для загрузки изображения
				using (var imageStream = new MemoryStream())
				{
					await UploadedFile.CopyToAsync(imageStream);

					geoTags = ExtractGeoTags(imageStream); // Извлечение геометок


					// Уменьшаем изображение перед загрузкой
					imageStream.Position = 0; // Сброс позиции потока перед изменением размера

					using (var image = Image.FromStream(imageStream))
					{
						var resizedImage = ResizeImage(image, 400); // Уменьшаем размер изображения                        						
						resizedImage.Save(fullPath, ImageFormat.Jpeg);// Сохраняем уменьшенное изображение в файл
					}
				}

				// Создаем новый объект FileModel, добавляем переменную geoTags
				FileModel newFile = new FileModel
				{
					Name = UploadedFile.FileName,
					Path = path,
					Latitude = geoTags.Latitude,
					Longitude = geoTags.Longitude
				};

				_context.Files.Add(newFile);
				await _context.SaveChangesAsync();

				return RedirectToPage("./Index");
			}

			return Page();
		}



		/// <summary>
		/// Метод для извлечения геолокации из адреса по строке
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private GeoTags ExtractGeoTags(string filePath)
		{
			GeoTags geoTags = new GeoTags();

			try
			{
				// Создаем новый объект FileModel, используя переменную geoTags
				var directories = ImageMetadataReader.ReadMetadata(filePath);
				var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();

				if (gpsDirectory!=null)
				{
					var location = gpsDirectory.GetGeoLocation();

					if (location!=null)
					{
						geoTags.Latitude = location.Latitude;
						geoTags.Longitude = location.Longitude;
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error during extraction of geo tag: {ex.Message}");
			}

			return geoTags;
		}


		/// <summary>
		/// Метод для извлечения геолокации из потока
		/// </summary>
		/// <param name="imageStream"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private GeoTags ExtractGeoTags(Stream imageStream)
		{
			GeoTags geoTags = new GeoTags();

			try
			{
				imageStream.Position = 0;// Сброс позиции потока перед чтением метаданных

				var directories = ImageMetadataReader.ReadMetadata(imageStream); // Читаем метаданные файла
				var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault(); // Ищем EXIF-теги с геолокацией

				if (gpsDirectory!=null)
				{
					var location = gpsDirectory.GetGeoLocation();

					if (location!=null)
					{
						geoTags.Latitude = location.Latitude;
						geoTags.Longitude = location.Longitude;
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error during extraction of geo tag: {ex.Message}");
			}

			return geoTags;
		}


		/// <summary>
		/// Метод для уменьшения размера фотографии
		/// </summary>
		/// <param name="image"></param>
		/// <param name="resizedWidth"></param>
		/// <returns></returns>
		private Bitmap ResizeImage(Image image, int resizedWidth)
		{
			// Вычисляем новую высоту, сохраняя пропорции
			var newWidth = resizedWidth;
			var newHeight = (int)(image.Height * ((double)newWidth / image.Width));

			// Создаём новое изображение с изменёнными размерами
			var newImage = new Bitmap(newWidth, newHeight);

			using (var graphics = Graphics.FromImage(newImage))
			{
				graphics.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality;
				graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				// Изображение с новым размером
				graphics.DrawImage(image, 0, 0, newWidth, newHeight);
			}

			return newImage;
		}

	}
}
