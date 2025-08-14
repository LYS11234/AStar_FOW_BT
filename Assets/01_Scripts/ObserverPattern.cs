using UnityEngine;

public interface IObserver
{
    void OnNotify(byte eventType, object data);

    void OnNotify(byte eventType, object data0, object data1);
}

public interface ISubject
{
    void RegisterObserver(IObserver observer);
    void UnregisterObserver(IObserver observer);
    void NotifyObservers(byte eventType, object data);

    void NotifyObservers(byte eventType, object data0, object data1);
}
