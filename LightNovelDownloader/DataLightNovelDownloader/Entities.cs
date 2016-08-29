using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLightNovelDownloader
{
    public class Book
    {
        //public int BookId { get; set; }
        [Key]
        public string Name { get; set; }
        public string IndexUrl { get; set; }
        public IEnumerable<Chapter> Chapters { get; set; }
    }
    public class Chapter
    {
        public int ChapterId { get; set; }
        public string ChapterUrl { get; set; }
        public string DisplayName { get; set; }
        public string Content { get; set; }
        public virtual Book Book { get; set; }
        [ForeignKey("Book")]
        public string Name { get; set; }
    }
}
