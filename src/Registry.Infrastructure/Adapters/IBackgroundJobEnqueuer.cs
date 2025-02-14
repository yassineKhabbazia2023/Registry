using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Adapters
{
    public interface IBackgroundJobEnqueuer
    {
        string Enqueue<T>(Expression<Action<T>> methodCall);
    }
}
