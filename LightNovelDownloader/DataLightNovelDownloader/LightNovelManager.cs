using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLightNovelDownloader
{
    public class LightNovelManager
    {
        public LightNovelRepository<Book> RepBook { get; } = new LightNovelRepository<Book>();
        public LightNovelRepository<Chapter> RepChapter { get; } = new LightNovelRepository<Chapter>();
    }
}
