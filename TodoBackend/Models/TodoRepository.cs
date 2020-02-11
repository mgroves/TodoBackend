using System.Collections.Generic;
using Couchbase;
using Couchbase.Core;
using Couchbase.Extensions.DependencyInjection;
using Couchbase.N1QL;

namespace TodoBackend.Models
{
    public interface ITodoRepository
    {
        List<TodoItem> GetAll();
        void AddNew(TodoItem item);
        TodoItem Get(string id);
        void Delete(string id);
        void DeleteAll();
        void Update(TodoItem todo);
    }

    public class TodoRepository : ITodoRepository
    {
        private readonly IBucket _bucket;

        public TodoRepository(IBucketProvider bucketProvider)
        {
            _bucket = bucketProvider.GetBucket("todo");

            EnsureIndex();
        }

        private void EnsureIndex()
        {
            var manager = _bucket.CreateManager();
            manager.CreateN1qlPrimaryIndex(defer: false);
        }

        public List<TodoItem> GetAll()
        {
            var n1ql = @"SELECT t.*, META(t).id FROM todo t";
            var query = QueryRequest.Create(n1ql);
            query.ScanConsistency(ScanConsistency.RequestPlus);
            var result = _bucket.Query<TodoItem>(query);
            return result.Rows;
        }

        public void AddNew(TodoItem item)
        {
            _bucket.Insert(new Document<TodoItem>
            {
                Id = item.Id,
                Content = item
            });
        }

        public TodoItem Get(string id)
        {
            var result = _bucket.Get<TodoItem>(id);
            var doc = result.Value;
            doc.Id = id;
            return doc;
        }

        public void Delete(string id)
        {
            _bucket.Remove(id);
        }

        public void DeleteAll()
        {
            var n1ql = @"DELETE FROM todo t";
            var query = QueryRequest.Create(n1ql);
            query.ScanConsistency(ScanConsistency.RequestPlus);
            _bucket.Query<dynamic>(query);
        }

        public void Update(TodoItem todo)
        {
            var expr = _bucket.MutateIn<TodoItem>(todo.Id);

            if (todo.Completed.HasValue)
                expr = expr.Upsert("completed", todo.Completed);
            if (!string.IsNullOrEmpty(todo.Title))
                expr = expr.Upsert("title", todo.Title);
            if (todo.Order.HasValue)
                expr = expr.Upsert("order", todo.Order.Value);
            expr.Execute();
        }
    }
}