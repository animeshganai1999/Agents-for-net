﻿using Azure.Core;
using Microsoft.Agents.Extensions.Teams.AI.Utilities;

namespace Microsoft.Agents.Extensions.Teams.AI.Embeddings
{
    /// <summary>
    /// Options for configuring an `OpenAIEmbeddings` to generate embeddings using an Azure OpenAI hosted model.
    /// </summary>
    public class AzureOpenAIEmbeddingsOptions : BaseOpenAIEmbeddingsOptions
    {
        /// <summary>
        /// API key to use when making requests to Azure OpenAI.
        /// </summary>
        public string? AzureApiKey { get; set; }

        /// <summary>
        /// The token credential to use when making requests to Azure OpenAI.
        /// </summary>
        public TokenCredential? TokenCredential { get; set; }

        /// <summary>
        /// Name of the Azure OpenAI deployment (model) to use.
        /// </summary>
        public string AzureDeployment { get; set; }

        /// <summary>
        /// Deployment endpoint to use.
        /// </summary>
        public string AzureEndpoint { get; set; }

        /// <summary>
        /// Optional. Version of the API being called.
        /// </summary>
        public string? AzureApiVersion { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureOpenAIEmbeddingsOptions"/> class.
        /// </summary>
        /// <param name="azureApiKey">API key to use when making requests to Azure OpenAI.</param>
        /// <param name="azureDeployment">Name of the Azure OpenAI deployment (model) to use.</param>
        /// <param name="azureEndpoint">Deployment endpoint to use.</param>
        public AzureOpenAIEmbeddingsOptions(
            string azureApiKey,
            string azureDeployment,
            string azureEndpoint) : base()
        {
            Verify.ParamNotNull(azureApiKey);
            Verify.ParamNotNull(azureDeployment);
            Verify.ParamNotNull(azureEndpoint);

            azureEndpoint = azureEndpoint.Trim();

            this.AzureApiKey = azureApiKey;
            this.AzureDeployment = azureDeployment;
            this.AzureEndpoint = azureEndpoint;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureOpenAIEmbeddingsOptions"/> class.
        /// </summary>
        /// <param name="tokenCredential">token credential</param>
        /// <param name="azureDefaultDeployment">the deployment name</param>
        /// <param name="azureEndpoint">azure endpoint</param>
        public AzureOpenAIEmbeddingsOptions(TokenCredential tokenCredential, string azureDefaultDeployment, string azureEndpoint)
        {
            Verify.ParamNotNull(tokenCredential);
            Verify.ParamNotNull(azureDefaultDeployment);
            Verify.ParamNotNull(azureEndpoint);

            this.TokenCredential = tokenCredential;
            this.AzureDeployment = azureDefaultDeployment;
            this.AzureEndpoint = azureEndpoint;
        }
    }
}

