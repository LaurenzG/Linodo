using DataLightNovelDownloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dto
{
    public class BookDto
    {
        public string Name { get; set; }
        public string IndexUrl { get; set; }
    }
    public class ChapterGroup
    {
        public ChapterGroup()
        {

        }
        public int ChapterGroupId { get; set; }
        public string Title { get; set; }
        public string Order { get; set; }
    }
    public class GravityChapters
    {
        public int ChapterId { get; set; }
        public double Number { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
    }
    public class ChapterDto
    {
        public int ChapterId { get; set; }
        public string DisplayName { get; set; }
        public string ChapterUrl { get; set; }
    }
    public static class DtoExtension
    {
        public static BookDto toDto(this Book book)
        {
            return new BookDto
            {
                Name = book.Name,
                IndexUrl = book.IndexUrl
            };
        }
        public static ChapterDto toDto(this Chapter chapter)
        {
            return new ChapterDto
            {
                ChapterId= chapter.ChapterId,
                ChapterUrl=chapter.ChapterUrl,
                DisplayName=chapter.DisplayName
            };
        }
    }
}
