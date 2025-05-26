using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Wator.ParallelWator;

public class ParallelWatorWorld : IWatorWorld
{
  private readonly object _gridLock = new object();
  private readonly Random _random;
  private readonly int[,] _randomMatrix;
  private readonly byte[] _rgbValues;
  private readonly int _partitionSize;
  private readonly int _numPartitions;

  #region Properties
  public int Width { get; private set; }
  public int Height { get; private set; }
  public Animal[,] Grid { get; private set; }

  // Simulation parameters
  public int InitialFishPopulation { get; private set; }
  public int InitialFishEnergy { get; private set; }
  public int FishBreedTime { get; private set; }

  public int InitialSharkPopulation { get; private set; }
  public int InitialSharkEnergy { get; private set; }
  public int SharkBreedEnergy { get; private set; }
  #endregion

  public ParallelWatorWorld(Settings settings)
  {
    Width = settings.Width;
    Height = settings.Height;
    InitialFishPopulation = settings.InitialFishPopulation;
    InitialFishEnergy = settings.InitialFishEnergy;
    FishBreedTime = settings.FishBreedTime;
    InitialSharkPopulation = settings.InitialSharkPopulation;
    InitialSharkEnergy = settings.InitialSharkEnergy;
    SharkBreedEnergy = settings.SharkBreedEnergy;

    _rgbValues = new byte[Width * Height * 3];
    _random = new Random();
    Grid = new Animal[Width, Height];

    int processorCount = Environment.ProcessorCount;
    _numPartitions = processorCount * 2;
    _partitionSize = (Width * Height) / _numPartitions;
    if (_partitionSize < 100) _partitionSize = 100;
    _randomMatrix = GenerateRandomMatrix(Width, Height);
    InitializePopulation();
  }

  private void InitializePopulation()
  {
    for (int i = 0; i < Width; i++)
    {
      for (int j = 0; j < Height; j++)
      {
        int value = _randomMatrix[i, j];
        if (value < InitialFishPopulation)
        {
          Grid[i, j] = new Fish(this, new Point(i, j), _random.Next(0, FishBreedTime));
        }
        else if (value < InitialFishPopulation + InitialSharkPopulation)
        {
          Grid[i, j] = new Shark(this, new Point(i, j), InitialSharkEnergy);
        }
        else
        {
          Grid[i, j] = null;
        }
      }
    }
  }

  // Execute one time step of the simulation in parallel
  public void ExecuteStep()
  {
    RandomizeMatrix(_randomMatrix);
    var partitions = CreatePartitions();
    Parallel.ForEach(partitions, partition =>
    {
      ProcessPartition(partition);
    });

    Parallel.For(0, Width, i =>
    {
      for (int j = 0; j < Height; j++)
      {
        lock (_gridLock)
        {
          if (Grid[i, j] != null)
            Grid[i, j].Commit();
        }
      }
    });
  }

  private List<CellPartition> CreatePartitions()
  {
    var partitions = new List<CellPartition>();
    int totalCells = Width * Height;
            
    for (int startIndex = 0; startIndex < totalCells; startIndex += _partitionSize)
    {
      int endIndex = Math.Min(startIndex + _partitionSize, totalCells);
      partitions.Add(new CellPartition(startIndex, endIndex));
    }
            
    return partitions;
  }

  private void ProcessPartition(CellPartition partition)
  {
    for (int index = partition.StartIndex; index < partition.EndIndex; index++)
    {
      // Convert linear index to position in random matrix
      int matrixCol = index % Width;
      int matrixRow = index / Width;
                
      // Get the actual position from the random matrix
      int randomValue = _randomMatrix[matrixCol, matrixRow];
      int col = randomValue % Width;
      int row = randomValue / Width;

      lock (_gridLock)
      {
        if (Grid[col, row] != null && !Grid[col, row].Moved)
          Grid[col, row].ExecuteStep();
      }
    }
  }

  // Generate bitmap for the current state of the Wator world
  public Image<Rgba32> GenerateImage()
  {
    var bitmap = new Image<Rgba32>(Width, Height);
    try {
      var blue = Color.DarkBlue.ToPixel<Rgba32>();
      
      bitmap.ProcessPixelRows(accessor => {
        for (int y = 0; y < accessor.Height; y++) {
          Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
          for (int x = 0; x < pixelRow.Length; x++) {
            lock (_gridLock) {
              pixelRow[x] = Grid[x, y] == null ? blue : Grid[x, y].Color;
            }
          }
        }
      });
    }
    catch (Exception e) {
      Console.WriteLine($"Error generating image: {e.Message}");
    }
    return bitmap;
  }

  public Point[] GetNeighbors(Type type, Point position)
  {
    Point[] neighbors = new Point[4];
    int neighborIndex = 0;
    int i, j;

    lock (_gridLock)
    {
      // look north
      i = position.X;
      j = (position.Y + Height - 1) % Height;
      if ((type == null) && (Grid[i, j] == null))
      {
        neighbors[neighborIndex] = new Point(i, j);
        neighborIndex++;
      }
      else if ((type != null) && (type.IsInstanceOfType(Grid[i, j])))
      {
        if ((Grid[i, j] != null) && (!Grid[i, j].Moved))
        {  // ignore animals moved in the current iteration
          neighbors[neighborIndex] = new Point(i, j);
          neighborIndex++;
        }
      }
      // look east
      i = (position.X + 1) % Width;
      j = position.Y;
      if ((type == null) && (Grid[i, j] == null))
      {
        neighbors[neighborIndex] = new Point(i, j);
        neighborIndex++;
      }
      else if ((type != null) && (type.IsInstanceOfType(Grid[i, j])))
      {
        if ((Grid[i, j] != null) && (!Grid[i, j].Moved))
        {
          neighbors[neighborIndex] = new Point(i, j);
          neighborIndex++;
        }
      }
      // look south
      i = position.X;
      j = (position.Y + 1) % Height;
      if ((type == null) && (Grid[i, j] == null))
      {
        neighbors[neighborIndex] = new Point(i, j);
        neighborIndex++;
      }
      else if ((type != null) && (type.IsInstanceOfType(Grid[i, j])))
      {
        if ((Grid[i, j] != null) && (!Grid[i, j].Moved))
        {
          neighbors[neighborIndex] = new Point(i, j);
          neighborIndex++;
        }
      }
      // look west
      i = (position.X + Width - 1) % Width;
      j = position.Y;
      if ((type == null) && (Grid[i, j] == null))
      {
        neighbors[neighborIndex] = new Point(i, j);
        neighborIndex++;
      }
      else if ((type != null) && (type.IsInstanceOfType(Grid[i, j])))
      {
        if ((Grid[i, j] != null) && (!Grid[i, j].Moved))
        {
          neighbors[neighborIndex] = new Point(i, j);
          neighborIndex++;
        }
      }
    }

    // Create result array that only contains found cells
    Point[] result = new Point[neighborIndex];
    Array.Copy(neighbors, result, neighborIndex);
    return result;
  }

  // Select a random neighboring cell of the given position and type
  public Point SelectNeighbor(Type type, Point position)
  {
    Point[] neighbors = GetNeighbors(type, position);  // Find all neighbors of required type
    if (neighbors.Length > 1)
    {
      return neighbors[_random.Next(neighbors.Length)];  // Return random neighbor
    }
    else if (neighbors.Length == 1)
    {
      return neighbors[0];
    }
    else
    {
      return new Point(-1, -1);  // No neighbor found
    }
  }

  // Create a matrix containing all numbers from 0 to width * height in random order
  private int[,] GenerateRandomMatrix(int width, int height)
  {
    int[,] matrix = new int[width, height];

    int row = 0;
    int col = 0;
    for (int i = 0; i < matrix.Length; i++)
    {
      matrix[col, row] = i;
      col++;
      if (col >= width) { col = 0; row++; }
    }
    RandomizeMatrix(matrix);  // Shuffle
    return matrix;
  }

  // Shuffle values of the matrix
  private void RandomizeMatrix(int[,] matrix)
  {
    // Perform Knuth shuffle (http://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle)
    int width = matrix.GetLength(0);
    int height = matrix.GetLength(1);
    int temp, selectedRow, selectedCol;

    // Using a thread-safe random number generator for parallelism
    var localRandom = new Random(_random.Next());

    int row = 0;
    int col = 0;
    for (int i = 0; i < height * width; i++)
    {
      temp = matrix[col, row];

      // Select random element from remaining elements
      selectedRow = localRandom.Next(row, height);
      if (selectedRow == row) selectedCol = localRandom.Next(col, width);
      else selectedCol = localRandom.Next(width);

      // Swap
      matrix[col, row] = matrix[selectedCol, selectedRow];
      matrix[selectedCol, selectedRow] = temp;

      // Increment col and row
      col++;
      if (col >= width) { col = 0; row++; }
    }
  }
}

// Helper class to represent a partition of cells to be processed
public class CellPartition
{
  public int StartIndex { get; }
  public int EndIndex { get; }

  public CellPartition(int startIndex, int endIndex)
  {
    StartIndex = startIndex;
    EndIndex = endIndex;
  }
}