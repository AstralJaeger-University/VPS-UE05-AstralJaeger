namespace Quandl.UI {

    public struct QuandlEventArgs {
        public List<Tuple<string, float[], float[]>> Series;
        public TimeSpan Duration;
    }

    public interface IQuandlClient {
        public void Request();

        event EventHandler<QuandlEventArgs> RequestCompleted;
    }
}