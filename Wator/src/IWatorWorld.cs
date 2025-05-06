using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Wator {
  // interface for all implementations of the Wator world simulator
  public interface IWatorWorld {
    void ExecuteStep();
    Image<Rgba32> GenerateImage();
  }
}