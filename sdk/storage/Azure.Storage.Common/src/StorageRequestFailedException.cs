﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;

namespace Azure.Storage
{
    /// <summary>
    /// Provide helpful information about errors calling Azure Storage endpoints.
    /// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors
    public partial class StorageRequestFailedException : RequestFailedException
#pragma warning restore CA1032 // Implement standard exception constructors
    {
        /// <summary>
        /// Additional information helpful in debugging errors.
        /// </summary>
        public IDictionary<string, string> AdditionalInformation { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the x-ms-request-id header that uniquely identifies the
        /// request that was made and can be used for troubleshooting.
        /// </summary>
        public string RequestId { get; private set; }

        /// <summary>
        /// Create a new StorageRequestFailedException.
        /// </summary>
        /// <param name="response">Response of the failed request.</param>
        /// <param name="message">Summary of the failure.</param>
        public StorageRequestFailedException(Response response, string message = null)
            : this(response, message ?? response?.ReasonPhrase, null)
        {
        }

        /// <summary>
        /// Create a new StorageRequestFailedException.
        /// </summary>
        /// <param name="response">Response of the failed request.</param>
        /// <param name="message">Summary of the failure.</param>
        /// <param name="innerException">Inner exception.</param>
        public StorageRequestFailedException(Response response, string message, Exception innerException)
            : this(response, message ?? response?.ReasonPhrase, innerException, null)
        {
        }

        /// <summary>
        /// Create a new StorageRequestFailedException.
        /// </summary>
        /// <param name="response">Response of the failed request.</param>
        /// <param name="message">Summary of the failure.</param>
        /// <param name="innerException">Inner exception.</param>
        /// <param name="errorCode">Optional error code of the failure.</param>
        /// <param name="additionalInfo">Optional additional info about the failure.</param>
        internal StorageRequestFailedException(
            Response response,
            string message,
            Exception innerException,
            string errorCode,
            IDictionary<string, string> additionalInfo = null)
            : base(
                  response?.Status ?? throw Errors.ArgumentNull(nameof(response)),
                  CreateMessage(response, message ?? response?.ReasonPhrase, GetErrorCode(response, errorCode), additionalInfo),
                  GetErrorCode(response, errorCode), innerException)
        {
            if (additionalInfo != null)
            {
                AdditionalInformation = additionalInfo;
            }

            // Include the RequestId
            RequestId = response.Headers.TryGetValue(Constants.HeaderNames.RequestId, out var value) ? value : null;
        }

        private static string GetErrorCode(Response response, string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
            {
                response.Headers.TryGetValue(Constants.HeaderNames.ErrorCode, out errorCode);
            }

            return errorCode;
        }

        /// <summary>
        /// Create the exception's Message.
        /// </summary>
        /// <param name="message">The default message.</param>
        /// <param name="response">The error response.</param>
        /// <param name="errorCode">An optional error code.</param>
        /// <param name="additionalInfo">Optional additional information.</param>
        /// <returns>The exception's Message.</returns>
        private static string CreateMessage(
            Response response,
            string message,
            string errorCode,
            IDictionary<string, string> additionalInfo)
        {
            // Start with the message, status, and reason
            StringBuilder messageBuilder = new StringBuilder()
                .AppendLine(message)
                .Append("Status: ")
                .Append(response.Status.ToString(CultureInfo.InvariantCulture))
                .Append(" (")
                .Append(response.ReasonPhrase)
                .AppendLine(")");

            // Make the Storage ErrorCode especially prominent
            if (!string.IsNullOrEmpty(errorCode) ||
                response.Headers.TryGetValue(Constants.HeaderNames.ErrorCode, out errorCode))
            {
                messageBuilder
                    .AppendLine()
                    .Append("ErrorCode: ")
                    .AppendLine(errorCode);
            }

            // A Storage error's Content is (currently) always the ErrorCode and
            // AdditionalInfo, so we skip the specific Content section
            if (additionalInfo != null && additionalInfo.Count > 0)
            {
                messageBuilder
                    .AppendLine()
                    .AppendLine("Additional Information:");
                foreach (KeyValuePair<string, string> info in additionalInfo)
                {
                    messageBuilder
                        .Append(info.Key)
                        .Append(": ")
                        .AppendLine(info.Value);
                }
            }

            // Include the response headers
            messageBuilder
                .AppendLine()
                .AppendLine("Headers:");
            foreach (HttpHeader responseHeader in response.Headers)
            {
                messageBuilder
                    .Append(responseHeader.Name)
                    .Append(": ")
                    .AppendLine(responseHeader.Value);
            }

            return messageBuilder.ToString();
        }
    }
}
