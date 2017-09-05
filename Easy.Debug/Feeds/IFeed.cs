using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.Debug.Feeds
{
    public interface IFeed
    {
        void Execute(IDictionary<string,string> criteria);
    }
}
