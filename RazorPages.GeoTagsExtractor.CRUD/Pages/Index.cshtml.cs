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

		public IEnumerable<FileModel> FilesCollection { get; set; } = default!; // �������� ��� �������� ��������� ����������� ������, ������� ����������� �� ���� ������

		private readonly ApplicationContext _context;
		private readonly IWebHostEnvironment _environment;

        public IndexModel(ApplicationContext context, IWebHostEnvironment environment)
        {
			_context = context;
			_environment = environment;
		}

		/// <summary>
		/// ����� ��� ��������� ������ ���� ������ �� ���� ������ � �������� �� � ������������� �������� ��� �����������
		/// </summary>
		/// <returns></returns>
		public async Task OnGetAsync()
		{
			FilesCollection=await _context.Files.ToListAsync();
		}

		[BindProperty]
		public IFormFile UploadedFile { get; set; } = default!; // �������� ��� �������� ������������ ����� �� ����� �� ��������


		public async Task<IActionResult> OnPostAsync()
		{
			if (UploadedFile != null)
			{
				// ������ ���� � �����
				string path = Path.Combine("Files", UploadedFile.FileName);
				string fullPath = Path.Combine(_environment.WebRootPath, path);

				GeoTags geoTags = null; // ��������� ���������� geoTags (�� ����� using)

				// ������� ��������� ����� ��� �������� �����������
				using (var imageStream = new MemoryStream())
				{
					await UploadedFile.CopyToAsync(imageStream);

					geoTags = ExtractGeoTags(imageStream); // ���������� ��������


					// ��������� ����������� ����� ���������
					imageStream.Position = 0; // ����� ������� ������ ����� ���������� �������

					using (var image = Image.FromStream(imageStream))
					{
						var resizedImage = ResizeImage(image, 400); // ��������� ������ �����������                        						
						resizedImage.Save(fullPath, ImageFormat.Jpeg);// ��������� ����������� ����������� � ����
					}
				}

				// ������� ����� ������ FileModel, ��������� ���������� geoTags
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
		/// ����� ��� ���������� ���������� �� ������ �� ������
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private GeoTags ExtractGeoTags(string filePath)
		{
			GeoTags geoTags = new GeoTags();

			try
			{
				// ������� ����� ������ FileModel, ��������� ���������� geoTags
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
		/// ����� ��� ���������� ���������� �� ������
		/// </summary>
		/// <param name="imageStream"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private GeoTags ExtractGeoTags(Stream imageStream)
		{
			GeoTags geoTags = new GeoTags();

			try
			{
				imageStream.Position = 0;// ����� ������� ������ ����� ������� ����������

				var directories = ImageMetadataReader.ReadMetadata(imageStream); // ������ ���������� �����
				var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault(); // ���� EXIF-���� � �����������

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
		/// ����� ��� ���������� ������� ����������
		/// </summary>
		/// <param name="image"></param>
		/// <param name="resizedWidth"></param>
		/// <returns></returns>
		private Bitmap ResizeImage(Image image, int resizedWidth)
		{
			// ��������� ����� ������, �������� ���������
			var newWidth = resizedWidth;
			var newHeight = (int)(image.Height * ((double)newWidth / image.Width));

			// ������ ����� ����������� � ���������� ���������
			var newImage = new Bitmap(newWidth, newHeight);

			using (var graphics = Graphics.FromImage(newImage))
			{
				graphics.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality;
				graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				// ����������� � ����� ��������
				graphics.DrawImage(image, 0, 0, newWidth, newHeight);
			}

			return newImage;
		}

	}
}
