using Quandl.API;
using System.Diagnostics;

namespace Quandl.UI {
    public class TaskClient : IQuandlClient {
        private readonly QuandlService service = new QuandlService();
        private readonly string[] names = { "MSFT", "AAPL", "GOOG" };
        private const int INTERVAL = 200;

        #region task-based implementation
        public void Request() {
          var series = new List<Tuple<string, float[], float[]>>();
          var sw = new Stopwatch();
          sw.Start();
            
          var allTasks = new List<Task<Tuple<string, float[], float[]>>>();
            
          foreach (var name in names) {
            var retrieveTask = Task.Factory.StartNew(() => RetrieveStockData(name));
            var processTask = retrieveTask.ContinueWith(previousTask => {
              StockData sd = previousTask.Result;
              List<StockValue> values = sd.GetValues();
              return Tuple.Create(name, GetSeries(values, name), GetTrend(values, name));
            });
            allTasks.Add(processTask);
          }
          
          Task.Factory.ContinueWhenAll(allTasks.ToArray(), completedTasks => {
            foreach (var task in completedTasks) {
              series.Add(task.Result);
            }
                
            sw.Stop();
            OnRequestCompleted(series, sw.Elapsed);
          });

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