using DataLightNovelDownloader;
using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace RESTLightNovelService.Controllers
{
    public class ChapterController : ApiController
    {
        // GET api/values
        LightNovelManager man = new LightNovelManager();
        public IEnumerable<ChapterDto> Get()
        {
            return man.RepChapter.Get().Select(b => b.toDto()).ToList();
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post(Chapter c)
        {
            try
            {
                man.RepChapter.Update(c);
            }
            catch (Exception)
            {
                man.RepChapter.Create(c);
            }
        }

        // PUT api/values/5
        public void Put(int id, ChapterDto c)
        {
            man.RepChapter.Create(new Chapter
            {
                ChapterId=c.ChapterId,
                ChapterUrl=c.ChapterUrl
            });
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
