using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataLightNovelDownloader.EF
{
    interface IRepository
    {
        void Create<T>(T obj) where T : class;
        void Update<T>(T obj) where T : class;
        void Delete<T>(T obj) where T : class;
        void DeleteAll<T>() where T : class;
        IEnumerable<T> Get<T>(Expression<Func<T, bool>> filter = null, params Expression<Func<T, object>>[] includes) where T : class;
        T GetByKey<T>(object key) where T : class;
    }
}
