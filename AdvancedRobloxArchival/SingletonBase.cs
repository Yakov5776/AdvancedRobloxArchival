namespace AdvancedRobloxArchival
{
    public class SingletonBase<TSingleton> 
        where TSingleton : class, new()
    {
        public static readonly TSingleton Singleton = new TSingleton();
    }
}
