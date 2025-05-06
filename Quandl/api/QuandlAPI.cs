namespace Quandl.API {
  public interface QuandlAPI {
    StockData GetData(string identifier);
  }
}
