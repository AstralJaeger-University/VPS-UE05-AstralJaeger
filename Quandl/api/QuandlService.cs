using System.Xml.Serialization;

namespace Quandl.API {
  public class QuandlService : QuandlAPI {
    public StockData GetData(string identifier) {
      StockData data = null;
      using (var fs = new FileStream($@"Data/{identifier}.xml", FileMode.Open, FileAccess.Read)) {
        var deserializer = new XmlSerializer(typeof(StockData));
        data = (StockData)deserializer.Deserialize(fs);
      }

      // simulate request latency
      var random = new Random();
      Thread.Sleep(random.Next(2000, 3000));

      return data;
    }
  }
}
