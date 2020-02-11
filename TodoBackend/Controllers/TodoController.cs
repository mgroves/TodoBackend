using System;
using System.Linq;
using Couchbase;
using Couchbase.Core;
using Couchbase.Extensions.DependencyInjection;
using Couchbase.N1QL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TodoBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly IBucket _bucket;

        public TodoController(IBucketProvider bucketProvider)
        {
            _bucket = bucketProvider.GetBucket("todo");

            EnsureIndex();
        }

        private void EnsureIndex()
        {
            var manager = _bucket.CreateManager();
            manager.CreateN1qlPrimaryIndex(defer: false);
        }

        [Route("/")]
        [HttpGet]
        public IActionResult Home()
        {
            var n1ql = @"SELECT t.*, META(t).id FROM todo t";
            var query = QueryRequest.Create(n1ql);
            query.ScanConsistency(ScanConsistency.RequestPlus);
            var result = _bucket.Query<TodoItem>(query);
            var views = result.Rows.Select(r => new TodoItemView(r, Request));
            return Ok(views);
        }

        [Route("/")]
        [HttpPost]
        public IActionResult NewTodo(TodoItem item)
        {
            item.Id = Guid.NewGuid().ToString();
            item.Completed = false;
            _bucket.Insert(new Document<TodoItem>
            {
                Id = item.Id,
                Content = item
            });
            return Ok(new TodoItemView(item, Request));
        }

        [Route("/{id}")]
        [HttpGet]
        public IActionResult GetTodo(string id)
        {
            var result = _bucket.Get<TodoItem>(id);
            var doc = result.Value;
            doc.Id = id;
            return Ok(new TodoItemView(doc, Request));
        }

        [Route("/{id?}")]
        [HttpDelete]
        public IActionResult DeleteTodo(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _bucket.Remove(id);
                return Ok();
            }
            var n1ql = @"DELETE FROM todo t";
            var query = QueryRequest.Create(n1ql);
            query.ScanConsistency(ScanConsistency.RequestPlus);
            _bucket.Query<dynamic>(query);
            return Ok();
        }

        [Route("/{id}")]
        [HttpPatch]
        public IActionResult ChangeTodo(string id, TodoItem item)
        {
            var expr = _bucket.MutateIn<TodoItem>(id);

            if (item.Completed.HasValue)
                expr = expr.Upsert("completed", item.Completed);
            if (!string.IsNullOrEmpty(item.Title))
                expr = expr.Upsert("title", item.Title);
            if (item.Order.HasValue)
                expr = expr.Upsert("order", item.Order.Value);
            expr.Execute();

            var result = _bucket.Get<TodoItem>(id);
            var doc = result.Value;
            doc.Id = id;
            return Ok(new TodoItemView(doc, Request));
        }
    }

    /// <summary>
    /// This class is meant to return the TodoItem to the public
    /// </summary>
    public class TodoItemView
    {
        public TodoItemView(TodoItem item, HttpRequest req)
        {
            Title = item.Title;
            Completed = item.Completed ?? false;
            Url = req.Scheme + "://" + req.Host + "/" + item.Id;
            Order = item.Order;
        }

        public string Title { get; }
        public bool Completed { get; }
        public string Url { get; }
        public int? Order { get; }
    }

    /// <summary>
    /// This class is meant to model the TodoItem for the database.
    /// The Id value is NOT serialized within the document, it's only stored
    /// as the document key
    /// </summary>
    public class TodoItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public bool? Completed { get; set; }
        public int? Order { get; set; }

        public bool ShouldSerializeId() => false;
        public bool ShouldDeserializeId() => true;
    }
}
