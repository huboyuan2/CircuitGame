using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public int width;
    public int height;
    public CircuitBlock[] blocks;

    public CircuitBlock GetBlock(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;
        return blocks[y * width + x];
    }

    public void SetBlock(int x, int y, CircuitBlock block)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;
        blocks[y * width + x] = block;
    }

    // swap the position in block array and Transform.position(use origtinal position)
    public bool SwapBlocks(CircuitBlock block1, CircuitBlock block2, Vector3 block1OriginalPosition)
    {
        if (block1 == null || block2 == null)
            return false;

        int index1 = Array.IndexOf(blocks, block1);
        int index2 = Array.IndexOf(blocks, block2);

        if (index1 == -1 || index2 == -1)
            return false;

        // C# swap reference position in array
        (blocks[index1], blocks[index2]) = (blocks[index2], blocks[index1]);

        //swap Transform.position in scene (use origtinal position)
        Vector3 block2Position = block2.transform.position;
        block1.transform.position = block2Position;
        block2.transform.position = block1OriginalPosition;

        return true;
    }

    public void Initialize(int w, int h)
    {
        width = w;
        height = h;
        blocks = new CircuitBlock[w * h];
    }
}

public class SudoMap : MonoBehaviour
{
    public MapData mapData = new MapData();

    void Start()
    {
        //mapData.Initialize(10, 10);
    }

    void Update()
    {
        
    }

    // public interface
    public bool TrySwapBlocks(CircuitBlock block1, CircuitBlock block2, Vector3 block1OriginalPosition)
    {
        return mapData.SwapBlocks(block1, block2, block1OriginalPosition);
    }
}