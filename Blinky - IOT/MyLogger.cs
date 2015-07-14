using att.iot.client;
using System.Diagnostics;

namespace Blinky
{
    internal class MyLogger : ILogger
    {
        //static Logger _logger = LogManager.GetCurrentClassLogger();


        /// <summary>
        /// Writes a diagnostic message at the trace level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args">any arguments to replace in the message.</param>
        public void Trace(string message, params object[] args)
        {
            //_logger.Trace(message, args);
            Debug.WriteLine("trace: " + message, args);
        }

        /// <summary>
        /// Writes a diagnostic message at the infor level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args">any arguments to replace in the message.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Info(string message, params object[] args)
        {
            //_logger.Info(message, args);
            Debug.WriteLine("Info: " + message, args);
        }

        /// <summary>
        /// Writes a diagnostic message at the warning level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args">any arguments to replace in the message.</param>
        public void Warn(string message, params object[] args)
        {
            //_logger.Warn(message, args);
            Debug.WriteLine("Warn: " + message, args);
        }

        /// <summary>
        /// Writes a diagnostic message at the error level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args">any arguments to replace in the message.</param>
        public void Error(string message, params object[] args)
        {
            //_logger.Error(message, args);
            Debug.WriteLine("Error: " + message, args);
        }
    }
}