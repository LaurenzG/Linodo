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
    public class BookController : ApiController
    {
        // GET api/values
        LightNovelManager man = new LightNovelManager();
        public IEnumerable<BookDto> Get()
        {
            return man.RepBook.Get().Select(b => b.toDto()).ToList();
        }

        // GET api/values/5
        public BookDto Get(string id)
        {
            return man.RepBook.Get().Where(b=>b.Name==id).Select(b=>b.toDto()).Single();
        }

        // POST api/values
        public void Post(Book book)
        {
            try {
                man.RepBook.Update(book);
            }
            catch (Exception)
            {
                man.RepBook.Create(book);
            }
        }

        // PUT api/values/5
        public void Put(string id, BookDto book)
        {
            man.RepBook.Create(new Book
            {
                Name=book.Name
            });
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
