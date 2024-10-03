namespace RazorPages.GeoTagsExtractor.CRUD.Models
{
    public class FileModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
