using UnityEngine;

/// <summary>
/// Heap binario minimo para A*. Guarda pares (nodo, prioridad) y permite
/// prioridades duplicadas; A* descarta las entradas viejas al cerrar nodos.
/// </summary>
public sealed class MinHeap
{
    private int[] _nodes;
    private float[] _priorities;
    private int _count;

    public int Count => _count;

    public MinHeap(int capacity)
    {
        capacity = Mathf.Max(1, capacity);
        _nodes = new int[capacity];
        _priorities = new float[capacity];
    }

    public void Push(int node, float priority)
    {
        if (_count == _nodes.Length) Resize(_count * 2);

        int index = _count;
        _count++;

        _nodes[index] = node;
        _priorities[index] = priority;
        SiftUp(index);
    }

    public int Pop()
    {
        int result = _nodes[0];
        _count--;

        if (_count > 0)
        {
            _nodes[0] = _nodes[_count];
            _priorities[0] = _priorities[_count];
            SiftDown(0);
        }

        return result;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_priorities[parent] <= _priorities[index]) break;

            Swap(parent, index);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        while (true)
        {
            int left = index * 2 + 1;
            int right = left + 1;
            int smallest = index;

            if (left < _count && _priorities[left] < _priorities[smallest]) smallest = left;
            if (right < _count && _priorities[right] < _priorities[smallest]) smallest = right;
            if (smallest == index) break;

            Swap(smallest, index);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (_nodes[a], _nodes[b]) = (_nodes[b], _nodes[a]);
        (_priorities[a], _priorities[b]) = (_priorities[b], _priorities[a]);
    }

    private void Resize(int capacity)
    {
        System.Array.Resize(ref _nodes, capacity);
        System.Array.Resize(ref _priorities, capacity);
    }
}
