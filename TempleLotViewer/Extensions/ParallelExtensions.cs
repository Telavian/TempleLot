namespace TempleLotViewer.Extensions
{
    public static class ParallelExtensions
    {
        public static Task ForAllAsync<T>(this IEnumerable<T> items, Func<T, ValueTask> action)
        {
            var tasks = new List<Task>();

            foreach (var item in items)
            {
                var task = Task.Run(() => action(item));
                tasks.Add(task);
            }

            return Task.WhenAll(tasks);
        }

        public static void ForAll<T>(this IEnumerable<T> items, Action<T> action)
        {
            Parallel.ForEach(items, action);
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> action)
        {
            foreach (var item in items)
            {
                await action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }
    }
}
