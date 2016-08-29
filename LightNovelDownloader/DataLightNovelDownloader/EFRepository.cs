using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;

namespace DataLightNovelDownloader
{
    public interface IRepository<T>
    {
        void Create(T obj);
        void Update(T obj);
        void Delete(T obj);
        void DeleteAll();
        T GetByKey(object key);
        IEnumerable<T> Get(Expression<Func<T, bool>> predicate = null,
                           string strInclude = null);
    }

    public class EFRepository<T, Ctx> : IRepository<T> where T : class where Ctx : DbContext, new()
    {
        public void Create(T obj)
        {
            using (Ctx db = new Ctx())
            {
                db.Set<T>().Add(obj);
                db.SaveChanges();
            }
        }

        public void Delete(T obj)
        {
            using (Ctx db = new Ctx())
            {
                db.Set<T>().Attach(obj);
                db.Set<T>().Remove(obj);
                db.SaveChanges();
            }
        }

        public void DeleteAll()
        {
            using (Ctx db = new Ctx())
            {
                db.Set<T>().RemoveRange(db.Set<T>());
            }
        }

        public IEnumerable<T> Get(Expression<Func<T, bool>> predicate = null,
                                  string strInclude = null)
        {
            using (Ctx db = new Ctx())
            {
                IQueryable<T> result = db.Set<T>();
                // filter
                if (predicate != null)
                    result = result.Where(predicate);
                // include navigation property
                if (strInclude != null)
                    foreach (var prop in strInclude.Split(','))
                        result = result.Include(prop);
                // disable lazy-loading
                return result.ToList();
            }
        }

        public T GetByKey(object key)
        {
            using (Ctx db = new Ctx())
            {
                return db.Set<T>().Find(key);
            }
        }

        public void Update(T obj)
        {
            using (Ctx db = new Ctx())
            {
                db.Set<T>().Attach(obj);
                db.Entry<T>(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }
}
