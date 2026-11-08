
namespace Arosoul.ContextFramework.ThirdParty {

    public interface IEvent { }

    public struct TestEventNoArgs : IEvent { }

    public struct TestEventWithArgs : IEvent { 
        public float coolFloat;
        public string coolString;
    }

}