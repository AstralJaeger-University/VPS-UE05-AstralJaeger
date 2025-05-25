using Quandl.API;
using System.Diagnostics;

namespace Quandl.UI {
    public class AsyncClient : IQuandlClient {
        private readonly QuandlService service = new QuandlService();
        private readonly string[] names = { "MSFT", "AAPL", "GOOG" };
        private const int INTERVAL = 200;

        #region async-based implementation
        public async Task RequestAsync() {
          var series = new List<Tuple<string, float[], float[]>>();
          var sw = new Stopwatch();
          sw.Start();
          
          var stockProcessingTasks = names.Select(name => ProcessStockDataAsync(name)).ToList();
          var results = await Task.WhenAll(stockProcessingTasks);
          series.AddRange(results);

          sw.Stop();
          OnRequestCompleted(series, sw.Elapsed);
        }

        private async Task<Tuple<string, float[], float[]>> ProcessStockDataAsync(string name) {
          StockData sd = await Task.Run(() => RetrieveStockData(name));
          List<StockValue> stockValues = sd.GetValues();
          Task<float[]> seriesTask = Task.Run(() => GetSeries(stockValues, name));
          Task<float[]> trendTask = Task.Run(() => GetTrend(stockValues, name));
          await Task.WhenAll(seriesTask, trendTask);
          float[] seriesData = await seriesTask;
          float[] trendData = await trendTask;

          return Tuple.Create(name, seriesData, trendData);
        }


        public async void Request() {
            await RequestAsync();
        }

        public event EventHandler<QuandlEventArgs>? RequestCompleted;
        private void OnRequestCompleted(List<Tuple<string, float[], float[]>> series, TimeSpan duration) {
            RequestCompleted?.Invoke(this, new QuandlEventArgs { Series = series, Duration = duration });
        }

        private StockData RetrieveStockData(string name) {
            return service.GetData(name);
        }

        private float[] GetTrend(List<StockValue> stockValues, string name) {
            double k, d;
            var trend = new float[INTERVAL];

            var vals = stockValues.Skip(stockValues.Count() - INTERVAL).Select(x => x.Close).ToArray();
            LinearLeastSquaresFitting.Calculate(vals, out k, out d);

            int j = 0;
            for (int i = 0; i < INTERVAL; i++) {
                trend[j] = (float)(k * i + d);
                ++j;
            }
            return trend;
        }

        private float[] GetSeries(List<StockValue> stockValues, string name) {
            int j = 0;
            var values = new float[INTERVAL];
            for (int i = stockValues.Count - INTERVAL; i < stockValues.Count; i++) {
                values[j] = (float)stockValues[i].Close;
                j++;
            }
            return values;
        }
        #endregion
    }
}