using System.Threading;
using System.Threading.Tasks;

namespace CustomControlLibrary
{
    /// <summary>
    /// Provides a exchange currency rate
    /// </summary>
    public interface IExchangeRateProvider
    {
        /// <summary>
        /// Return exchange currency rate.
        /// </summary>
        /// <param name="fromCurrency">Currency code to convert from.</param>
        /// <param name="toCurrency">Currency code to convert to.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>Returns the exchange rate if operation succeeded otherwise <see cref="double.NaN"/></returns>
        Task<double> GetExchangeRate(string fromCurrency, string toCurrency, CancellationToken cancellationToken);

        /// <summary>
        /// Dispose object
        /// </summary>
        public void Dispose();
    }
}