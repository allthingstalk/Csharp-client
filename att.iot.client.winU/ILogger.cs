using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace att.iot.client
{
    /// <summary>
    /// Implement this interface so that the <see cref="Server"/> class can log trace, info, warning and error messages to the desired output.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes a diagnostic message at the trace level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="value">The message to log.</param>
        /// <param name="args">any arguments to replace in the message.</param>
        void Trace(string message, params object[] args);

        /// <summary>
        /// Writes a diagnostic message at the infor level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="value">The message to log.</param>
        /// <param name="args">any arguments to replace in the message.</param>
        void Info(string message, params object[] args);

        /// <summary>
        /// Writes a diagnostic message at the warning level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="value">The message to log.</param>
        /// <param name="args">any arguments to replace in the message.</param>
        void Warn(string message, params object[] args);

        /// <summary>
        /// Writes a diagnostic message at the error level to the desired output using the specified arguments.
        /// </summary>
        /// <param name="value">The message to log.</param>
        /// <param name="args">any arguments to replace in the message.</param>
        void Error(string message, params object[] args);
    }
}
