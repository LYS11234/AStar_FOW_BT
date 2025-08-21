using System;

public class PriorityQueue<T> where T : IComparable<T>
{
    private T[] data;
    public int Count { get; private set; }
    public int MaxCount { get; private set; }

    public PriorityQueue()
    {
        MaxCount = 1;

        Count = 0;
        data = new T[MaxCount];
    }
    public PriorityQueue(int maxCount = int.MaxValue)
    {
        MaxCount = maxCount;
        Count = 0;
        data = new T[MaxCount];
    }

    private void Expand()
    {
        MaxCount *= 2;
        T[] newData = new T[MaxCount];
        Array.Copy(data, newData, Count);
        data = newData;
    }

    private void Swap(ref T left, ref T right)
    {
        T temp = left;
        left = right;
        right = temp;
    }

    public void Enqueue(T item)
    {
        if (Count == MaxCount)
        {
            Expand();
        }
        data[Count] = item; // 새 아이템을 배열의 끝에 추가
        Count++;

        int index = Count - 1;
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (data[index].CompareTo(data[parentIndex]) > 0)
            {
                break; // 부모 노드가 더 크면 종료
            }
            Swap(ref data[index], ref data[parentIndex]);
            index = parentIndex;
        }
    }
    public T Dequeue() // 큐에서 가장 작은 요소를 제거하고 반환합니다.
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue is empty");
        }
        T result = data[0];
        Count--;
        data[0] = data[Count];
        data[Count] = default(T); // 마지막 요소를 기본값으로 설정
        int index = 0;
        while (index < Count)
        {
            int leftChildIndex = index * 2 + 1;
            int rightChildIndex = index * 2 + 2;

            int next = index; // 현재 노드 인덱스
            if (leftChildIndex < Count && data[next].CompareTo(data[leftChildIndex]) > 0)
            {
                next = leftChildIndex; // 왼쪽 자식이 더 크면 왼쪽 자식 인덱스로 설정
            }
            if (rightChildIndex < Count && data[next].CompareTo(data[rightChildIndex]) > 0)
            {
                next = rightChildIndex; // 오른쪽 자식이 더 크면 오른쪽 자식 인덱스로 설정
            }
            if (index == next)
            {
                break; // 자식 노드가 없으면 종료
            }
            Swap(ref data[index], ref data[next]);
            index = next;
        }
        return result;
    }

    public T Peek()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue is empty");
        }
        return data[0];
    }



    public void Clear()
    {
        Count = 0; // 큐를 비우기 위해 Count를 0으로 설정
        Array.Clear(data, 0, data.Length); // 데이터 배열을 초기화
    }
}