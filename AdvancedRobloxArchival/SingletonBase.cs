using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedRobloxArchival
{
    public class SingletonBase<TSingleton> 
        where TSingleton : class, new()
    {
        public static readonly TSingleton Singleton = new TSingleton();
    }
}
