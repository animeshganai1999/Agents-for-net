﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Agents.Builder.TestBot.Shared.Dialogs;
using Microsoft.Agents.Builder.Testing;
using Microsoft.Agents.Builder.Testing.XUnit;
using Microsoft.Agents.Core.Models;
using Microsoft.BuilderSamples.Tests.Dialogs.TestData;
using Microsoft.BuilderSamples.Tests.Framework;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.BuilderSamples.Tests.Dialogs
{
    public class DateResolverDialogTests : BotTestBase
    {
        public DateResolverDialogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory]
        [MemberData(nameof(DateResolverDialogTestsDataGenerator.DateResolverCases), MemberType = typeof(DateResolverDialogTestsDataGenerator))]
        public async Task DialogFlowTests(TestDataObject testData)
        {
            // Arrange
            var testCaseData = testData.GetObject<DateResolverDialogTestCase>();
            var sut = new DateResolverDialog();
            var testClient = new DialogTestClient(Channels.Test, sut, testCaseData.InitialData, new[] { new XUnitDialogTestLogger(Output) });

            // Act/Assert
            Output.WriteLine($"Test Case: {testCaseData.Name}");
            Output.WriteLine($"\r\nDialog Input: {testCaseData.InitialData}");
            for (var i = 0; i < testCaseData.UtterancesAndReplies.GetLength(0); i++)
            {
                var reply = await testClient.SendActivityAsync<Activity>(testCaseData.UtterancesAndReplies[i, 0]);
                Assert.Equal(testCaseData.UtterancesAndReplies[i, 1], reply?.Text);
            }

            Output.WriteLine($"\r\nDialog result: {testClient.DialogTurnResult.Result}");
            Assert.Equal(testCaseData.ExpectedResult, testClient.DialogTurnResult.Result);
        }
    }
}
