using System.Collections;
using System.Threading.Tasks;

namespace TryEverything.UI
{
    class UnityTaskEnumerator : IEnumerable
    {
        public Task Task { get; }

        public object Current => null;

        public UnityTaskEnumerator(Task task)
        {
            Task = task;
        }

        public IEnumerator GetEnumerator()
        {
            while (!Task.IsCompleted)
            {
                yield return null;
            }
        }
    }
}
