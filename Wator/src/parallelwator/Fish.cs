using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Wator.ParallelWator;

public class Fish : Animal
{
  public override Rgba32 Color
  {
    get { return SixLabors.ImageSharp.Color.White.ToPixel<Rgba32>(); }
  }

  // create and initialize a new fish on the specified position of the given world
  public Fish(ParallelWatorWorld world, Point position, int age)
    : base(world, position)
  {
    Energy = world.InitialFishEnergy;
    Age = age;
  }

  // execute one simulation step for the fish
  // fish move around randomly and spawn when they reach a certain age
  public override void ExecuteStep()
  {
    // assert that the fish is moved only once in a simulation step
    if (Moved) throw new Exception("Tried to move a fish twice in one time step.");

    Age++;

    Point free = World.SelectNeighbor(null, Position);  // find a random empty neighboring cell
    if (free.X != -1) Move(free);  // empty cell found -> move there

    if (Age >= World.FishBreedTime) Spawn();  // fish reached breeding age -> spawn
  }

  // spawning behavior of fish
  protected override void Spawn()
  {
    Point free = World.SelectNeighbor(null, Position);  // find a random empty neighboring cell
    if (free.X != -1)
    {
      // empty neighboring cell found -> create new fish there
      Fish fish = new(World, free, 0);
      fish.Moved = true;
      // reduce the age of the parent fish to make sure it is allowed to
      // reproduce only every FishBreedTime steps
      Age -= World.FishBreedTime;
    }
  }
}