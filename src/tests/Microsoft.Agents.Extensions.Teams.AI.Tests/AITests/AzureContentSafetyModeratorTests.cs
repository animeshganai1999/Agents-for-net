﻿using Microsoft.Agents.Extensions.Teams.AI;
using Microsoft.Agents.Extensions.Teams.AI.Moderator;
using Microsoft.Agents.Extensions.Teams.AI.Planners;
using Microsoft.Agents.Extensions.Teams.AI.Prompts;
using Moq;
using System.Reflection;
using Microsoft.Agents.Extensions.Teams.AI.Tests.TestUtils;
using Microsoft.Agents.Builder;
using Azure.AI.ContentSafety;
using Azure;
using Microsoft.Agents.Extensions.Teams.AI.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Builder.State;

#pragma warning disable CS8604 // Possible null reference argument.
namespace Microsoft.Agents.Extensions.Teams.AI.Tests.AITests
{
    public class AzureContentSafetyModeratorTests
    {
        [Fact]
        public async Task Test_ReviewPrompt_ThrowsException()
        {
            // Arrange
            var apiKey = "randomApiKey";
            var endpoint = "https://test.cognitiveservices.azure.com";

            var botAdapterMock = new Mock<IChannelAdapter>();
            var activity = new Activity()
            {
                Text = "input",
            };

            var turnContext = TurnStateConfig.CreateConfiguredTurnContext();
            var turnStateMock = TurnStateConfig.GetTurnStateWithConversationStateAsync(turnContext);
            var promptTemplate = new PromptTemplate(
                "prompt",
                new(new() { })
            )
            {
                Configuration = new PromptTemplateConfiguration
                {
                    Completion =
                        {
                            MaxTokens = 2000,
                            Temperature = 0.2,
                            TopP = 0.5,
                        }
                }
            };

            var clientMock = new Mock<ContentSafetyClient>(new Uri(endpoint), new AzureKeyCredential(apiKey));
            var exception = new RequestFailedException("Exception Message");
            clientMock.Setup(client => client.AnalyzeTextAsync(It.IsAny<AnalyzeTextOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(exception);

            var options = new AzureContentSafetyModeratorOptions(apiKey, endpoint, ModerationType.Both);
            var moderator = new AzureContentSafetyModerator<TurnState>(options);
            moderator.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(moderator, clientMock.Object);

            // Act
            var result = await Assert.ThrowsAsync<RequestFailedException>(async () => await moderator.ReviewInputAsync(turnContext, turnStateMock.Result));

            // Assert
            Assert.Equal("Exception Message", result.Message);
        }

        [Theory]
        [InlineData(ModerationType.Input)]
        [InlineData(ModerationType.Output)]
        [InlineData(ModerationType.Both)]
        public async Task Test_ReviewPrompt_Flagged(ModerationType moderate)
        {
            // Arrange
            var apiKey = "randomApiKey";
            var endpoint = "https://test.cognitiveservices.azure.com";

            var botAdapterMock = new Mock<IChannelAdapter>();
            // TODO: when TurnState is implemented, get the user input
            var activity = new Activity()
            {
                Text = "input",
            };
            var turnContext = TurnStateConfig.CreateConfiguredTurnContext();
            var turnStateMock = await TurnStateConfig.GetTurnStateWithConversationStateAsync(turnContext);
            var promptTemplate = new PromptTemplate(
                "prompt",
                new(new() { })
            )
            {
                Configuration = new PromptTemplateConfiguration
                {
                    Completion =
                        {
                            MaxTokens = 2000,
                            Temperature = 0.2,
                            TopP = 0.5,
                        }
                }
            };

            var clientMock = new Mock<ContentSafetyClient>(new Uri(endpoint), new AzureKeyCredential(apiKey));
            var analyses = new List<TextCategoriesAnalysis>() { ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 2) };
            AnalyzeTextResult analyzeTextResult = ContentSafetyModelFactory.AnalyzeTextResult(null, analyses);
            Response? response = null;
            clientMock.Setup(client => client.AnalyzeTextAsync(It.IsAny<AnalyzeTextOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(analyzeTextResult, response));

            var options = new AzureContentSafetyModeratorOptions(apiKey, endpoint, moderate);
            var moderator = new AzureContentSafetyModerator<TurnState>(options);
            moderator.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(moderator, clientMock.Object);

            var expectedResult = new ModerationResult
            {
                Flagged = true,
                CategoriesFlagged = new()
                {
                    Hate = true,
                    HateThreatening = true
                },
                CategoryScores = new()
                {
                    Hate = 2 / 6.0,
                    HateThreatening = 2 / 6.0
                }
            };

            // Act
            var result = await moderator.ReviewInputAsync(turnContext, turnStateMock);

            // Assert
            if (moderate == ModerationType.Input || moderate == ModerationType.Both)
            {
                Assert.NotNull(result);
                Assert.Equal(AIConstants.DoCommand, result.Commands[0].Type);
                Assert.Equal(AIConstants.FlaggedInputActionName, ((PredictedDoCommand)result.Commands[0]).Action);
                Assert.NotNull(((PredictedDoCommand)result.Commands[0]).Parameters);
                Assert.True(((PredictedDoCommand)result.Commands[0]).Parameters!.ContainsKey("Result"));
                _AssertModerationResult(expectedResult, ((PredictedDoCommand)result.Commands[0]).Parameters!.GetValueOrDefault("Result"));
            }
            else
            {
                Assert.Null(result);
            }
        }

        [Theory]
        [InlineData(ModerationType.Input)]
        [InlineData(ModerationType.Output)]
        [InlineData(ModerationType.Both)]
        public async Task Test_ReviewPrompt_NotFlagged(ModerationType moderate)
        {
            // Arrange
            var apiKey = "randomApiKey";
            var endpoint = "https://test.cognitiveservices.azure.com";

            var botAdapterMock = new Mock<IChannelAdapter>();
            var activity = new Activity()
            {
                Text = "input",
            };
            var turnContext = TurnStateConfig.CreateConfiguredTurnContext();
            var turnStateMock = await TurnStateConfig.GetTurnStateWithConversationStateAsync(turnContext);
            var promptTemplate = new PromptTemplate(
                "prompt",
                new(new() { })
            )
            {
                Configuration = new PromptTemplateConfiguration
                {
                    Completion =
                        {
                            MaxTokens = 2000,
                            Temperature = 0.2,
                            TopP = 0.5,
                        }
                }
            };

            var clientMock = new Mock<ContentSafetyClient>(new Uri(endpoint), new AzureKeyCredential(apiKey));
            var analyses = new List<TextCategoriesAnalysis>() { ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 0) };
            AnalyzeTextResult analyzeTextResult = ContentSafetyModelFactory.AnalyzeTextResult(null, analyses); 
            Response? response = null;
            clientMock.Setup(client => client.AnalyzeTextAsync(It.IsAny<AnalyzeTextOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(analyzeTextResult, response));

            var options = new AzureContentSafetyModeratorOptions(apiKey, endpoint, moderate);
            var moderator = new AzureContentSafetyModerator<TurnState>(options);
            moderator.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(moderator, clientMock.Object);

            // Act
            var result = await moderator.ReviewInputAsync(turnContext, turnStateMock);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Test_ReviewPlan_ThrowsException()
        {
            // Arrange
            var apiKey = "randomApiKey";
            var endpoint = "https://test.cognitiveservices.azure.com";

            var turnContext = TurnStateConfig.CreateConfiguredTurnContext();
            var turnStateMock = TurnStateConfig.GetTurnStateWithConversationStateAsync(turnContext);
            var plan = new Plan(new List<IPredictedCommand>()
            {
                new PredictedDoCommand("action"),
                new PredictedSayCommand("response"),
            });

            var clientMock = new Mock<ContentSafetyClient>(new Uri(endpoint), new AzureKeyCredential(apiKey));
            var exception = new RequestFailedException("Exception Message");
            clientMock.Setup(client => client.AnalyzeTextAsync(It.IsAny<AnalyzeTextOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(exception);

            var options = new AzureContentSafetyModeratorOptions(apiKey, endpoint, ModerationType.Both);
            var moderator = new AzureContentSafetyModerator<TurnState>(options);
            moderator.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(moderator, clientMock.Object);

            // Act
            var result = await Assert.ThrowsAsync<RequestFailedException>(async () => await moderator.ReviewOutputAsync(turnContext, turnStateMock.Result, plan));

            // Assert
            Assert.Equal("Exception Message", result.Message);
        }

        [Theory]
        [InlineData(ModerationType.Input)]
        [InlineData(ModerationType.Output)]
        [InlineData(ModerationType.Both)]
        public async Task Test_ReviewPlan_Flagged(ModerationType moderate)
        {
            // Arrange
            var apiKey = "randomApiKey";
            var endpoint = "https://test.cognitiveservices.azure.com";

            var turnContext = TurnStateConfig.CreateConfiguredTurnContext();
            var turnStateMock = await TurnStateConfig.GetTurnStateWithConversationStateAsync(turnContext);
            var plan = new Plan(new List<IPredictedCommand>()
            {
                new PredictedDoCommand("action"),
                new PredictedSayCommand("response"),
            });

            var clientMock = new Mock<ContentSafetyClient>(new Uri(endpoint), new AzureKeyCredential(apiKey));
            var analyses = new List<TextCategoriesAnalysis>() { ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 2) };
            AnalyzeTextResult analyzeTextResult = ContentSafetyModelFactory.AnalyzeTextResult(null, analyses);
            Response? response = null;
            clientMock.Setup(client => client.AnalyzeTextAsync(It.IsAny<AnalyzeTextOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(analyzeTextResult, response));

            var options = new AzureContentSafetyModeratorOptions(apiKey, endpoint, moderate);
            var moderator = new AzureContentSafetyModerator<TurnState>(options);
            moderator.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(moderator, clientMock.Object);

            var expectedResult = new ModerationResult
            {
                Flagged = true,
                CategoriesFlagged = new()
                {
                    Hate = true,
                    HateThreatening = true
                },
                CategoryScores = new()
                {
                    Hate = 2 / 6.0,
                    HateThreatening = 2 / 6.0
                }
            };

            // Act
            var result = await moderator.ReviewOutputAsync(turnContext, turnStateMock, plan);

            // Assert
            if (moderate == ModerationType.Output || moderate == ModerationType.Both)
            {
                Assert.NotNull(result);
                Assert.Equal(AIConstants.DoCommand, result.Commands[0].Type);
                Assert.Equal(AIConstants.FlaggedOutputActionName, ((PredictedDoCommand)result.Commands[0]).Action);
                Assert.NotNull(((PredictedDoCommand)result.Commands[0]).Parameters);
                Assert.True(((PredictedDoCommand)result.Commands[0]).Parameters!.ContainsKey("Result"));
                _AssertModerationResult(expectedResult, ((PredictedDoCommand)result.Commands[0]).Parameters!.GetValueOrDefault("Result"));
            }
            else
            {
                Assert.StrictEqual(plan, result);
            }
        }

        [Theory]
        [InlineData(ModerationType.Input)]
        [InlineData(ModerationType.Output)]
        [InlineData(ModerationType.Both)]
        public async Task Test_ReviewPlan_NotFlagged(ModerationType moderate)
        {
            // Arrange
            var apiKey = "randomApiKey";
            var endpoint = "https://test.cognitiveservices.azure.com";

            var turnContext = TurnStateConfig.CreateConfiguredTurnContext();
            var turnStateMock = await TurnStateConfig.GetTurnStateWithConversationStateAsync(turnContext);
            var plan = new Plan(new List<IPredictedCommand>()
            {
                new PredictedDoCommand("action"),
                new PredictedSayCommand("response"),
            });

            var clientMock = new Mock<ContentSafetyClient>(new Uri(endpoint), new AzureKeyCredential(apiKey));
            var analyses = new List<TextCategoriesAnalysis>() { ContentSafetyModelFactory.TextCategoriesAnalysis(TextCategory.Hate, 0) };
            AnalyzeTextResult analyzeTextResult = ContentSafetyModelFactory.AnalyzeTextResult(null, analyses);
            Response? response = null;
            clientMock.Setup(client => client.AnalyzeTextAsync(It.IsAny<AnalyzeTextOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(analyzeTextResult, response));

            var options = new AzureContentSafetyModeratorOptions(apiKey, endpoint, moderate);
            var moderator = new AzureContentSafetyModerator<TurnState>(options);
            moderator.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(moderator, clientMock.Object);

            // Act
            var result = await moderator.ReviewOutputAsync(turnContext, turnStateMock, plan);

            // Assert
            Assert.StrictEqual(plan, result);
        }

        private static void _AssertModerationResult(ModerationResult expected, object actual)
        {
            Assert.NotNull(actual);
            var actualResult = (ModerationResult)actual;
            Assert.Equal(expected.Flagged, actualResult.Flagged);
            Assert.Equal(expected.CategoriesFlagged!.Hate, actualResult.CategoriesFlagged!.Hate);
            Assert.Equal(expected.CategoriesFlagged.HateThreatening, actualResult.CategoriesFlagged.HateThreatening);
            Assert.Equal(expected.CategoriesFlagged.SelfHarm, actualResult.CategoriesFlagged.SelfHarm);
            Assert.Equal(expected.CategoriesFlagged.Sexual, actualResult.CategoriesFlagged.Sexual);
            Assert.Equal(expected.CategoriesFlagged.Violence, actualResult.CategoriesFlagged.Violence);
            Assert.Equal(expected.CategoriesFlagged.ViolenceGraphic, actualResult.CategoriesFlagged.ViolenceGraphic);
            Assert.Equal(expected.CategoryScores!.Hate, actualResult.CategoryScores!.Hate, 10);
            Assert.Equal(expected.CategoryScores.HateThreatening, actualResult.CategoryScores.HateThreatening, 10);
            Assert.Equal(expected.CategoryScores.SelfHarm, actualResult.CategoryScores.SelfHarm, 10);
            Assert.Equal(expected.CategoryScores.Sexual, actualResult.CategoryScores.Sexual, 10);
            Assert.Equal(expected.CategoryScores.SexualMinors, actualResult.CategoryScores.SexualMinors, 10);
            Assert.Equal(expected.CategoryScores.Violence, actualResult.CategoryScores.Violence, 10);
            Assert.Equal(expected.CategoryScores.ViolenceGraphic, actualResult.CategoryScores.ViolenceGraphic, 10);
        }
    }
}
#pragma warning restore CS8604 // Possible null reference argument.
