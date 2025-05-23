﻿using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Agents.Extensions.Teams.AI.Models;
using Microsoft.Agents.Extensions.Teams.AI.Tests.TestUtils;
using Moq;
using OpenAI.Assistants;
using OpenAI.Files;

namespace Microsoft.Agents.Extensions.Teams.AI.Tests.AITests
{
    public class AssistantsMessageTest
    {
        [Fact]
        public void Test_Constructor()
        {
            // Arrange
            MessageContent content = OpenAIModelFactory.CreateMessageContent("message", "fileId");
            Mock<OpenAIFileClient> fileClientMock = new Mock<OpenAIFileClient>();
            fileClientMock.Setup(fileClient => fileClient.DownloadFileAsync("fileId", It.IsAny<CancellationToken>())).Returns(() =>
            {
                return Task.FromResult(ClientResult.FromValue(BinaryData.FromString("test"), new Mock<PipelineResponse>().Object));
            });
            fileClientMock.Setup(fileClient => fileClient.GetFileAsync("fileId", It.IsAny<CancellationToken>())).Returns(() =>
            {
                return Task.FromResult(ClientResult.FromValue(OpenAIModelFactory.CreateOpenAIFileInfo("fileId"), new Mock<PipelineResponse>().Object));
            });

            // Act
            AssistantsMessage assistantMessage = new AssistantsMessage(content, fileClientMock.Object);

            // Assert
            Assert.Equal(content, assistantMessage.MessageContent);
            Assert.Equal("message", assistantMessage.Content);
            Assert.Single(assistantMessage.AttachedFiles!);
            Assert.Equal("fileId", assistantMessage.AttachedFiles![0].FileInfo.Id);

            ChatMessage chatMessage = assistantMessage;
            Assert.NotNull(chatMessage);
        }
    }
}
