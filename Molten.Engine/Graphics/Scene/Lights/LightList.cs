using Molten.Collections;

namespace Molten.Graphics;

/// <summary>
/// A list 
/// </summary>
public class LightList
{
    public LightInstance[] Instances;
    public ValueFreeRefList<LightData> Data;    

    internal LightList(uint initialCapacity)
    {
        Data = new ValueFreeRefList<LightData>(initialCapacity);
        Instances = new LightInstance[initialCapacity];
    }

    public LightInstance New(ref LightData data)
    {
        uint id = Data.Add(ref data);
        if(id >= Instances.Length)
            Array.Resize(ref Instances, (int)Data.Capacity);

        Instances[id] = new LightInstance() { ID = id };

        return Instances[id];
    }

    public void Remove(LightInstance instance)
    {
        Data.RemoveAt(instance.ID);
    }
}

/// <summary>
/// Represents a light instance of any type. Acts as a container for extra data that is only needed CPU-side for preparation purposes, to avoid wasting valuable GPU bandwidth.
/// </summary>
public class LightInstance
{
    public float Range;
    public uint ID;
}
