using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MineSweeper;

public class MinefieldModel
{
    public int Width { get; }
    public int Height { get; }
    public int MineCount { get; }
    public bool MinesPlaced { get; private set; }

    public bool[,] MineMap { get; private set; }
    public int[,] AdjacentMineCounts { get; private set; }
    public HashSet<Vector2I> MinePositions { get; } = new();

    private readonly Random _random = new();

    public MinefieldModel(int width, int height, int mineCount) {
        Width = width;
        Height = height;
        MineCount = mineCount;
        MineMap = new bool[width, height];
        AdjacentMineCounts = new int[width, height];
    }

    public bool IsInBounds(int x, int y)
        => x >= 0 && x < Width && y >= 0 && y < Height;

    public List<Vector2I> GetNeighbors(int x, int y) {
        var neighbors = new List<Vector2I>(8);
        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                if (dx == 0 && dy == 0) {
                    continue;
                }
                int nx = x + dx;
                int ny = y + dy;
                if (IsInBounds(nx, ny)) {
                    neighbors.Add(new Vector2I(nx, ny));
                }
            }
        }

        return neighbors;
    }

    public void PlaceMines(Vector2I safeCell) {
        var safeZone = new HashSet<Vector2I> { safeCell };
        foreach (var n in GetNeighbors(safeCell.X, safeCell.Y)) {
            safeZone.Add(n);
        }

        int availableCells = Width * Height - safeZone.Count;
        int safeRadius = 1;
        while (availableCells < MineCount) {
            safeRadius++;
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    int dist = Math.Max(Math.Abs(x - safeCell.X), Math.Abs(y - safeCell.Y));
                    if (dist <= safeRadius)
                        safeZone.Add(new Vector2I(x, y));
                }
            }

            availableCells = Width * Height - safeZone.Count;
            if (safeRadius > Math.Max(Width, Height)) {
                break;
            }
        }

        var candidateCells = new List<Vector2I>();
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var pos = new Vector2I(x, y);
                if (!safeZone.Contains(pos)) {
                    candidateCells.Add(pos);
                }
            }
        }

        int cellCount = candidateCells.Count;
        int minesToPlace = Math.Min(MineCount, cellCount);
        for (int i = 0; i < minesToPlace; i++) {
            int j = _random.Next(i, cellCount);
            (candidateCells[i], candidateCells[j]) = (candidateCells[j], candidateCells[i]);
        }

        MinePositions.Clear();
        for (int i = 0; i < minesToPlace; i++) {
            var pos = candidateCells[i];
            MineMap[pos.X, pos.Y] = true;
            MinePositions.Add(pos);
        }

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (MineMap[x, y]) {
                    AdjacentMineCounts[x, y] = -1;
                } else {
                    int count = GetNeighbors(x, y).Count(nb => MineMap[nb.X, nb.Y]);
                    AdjacentMineCounts[x, y] = count;
                }
            }
        }

        MinesPlaced = true;
    }
}