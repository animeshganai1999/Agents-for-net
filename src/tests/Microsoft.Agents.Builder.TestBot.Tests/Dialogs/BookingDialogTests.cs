﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder.TestBot.Shared;
using Microsoft.Agents.Builder.TestBot.Shared.Dialogs;
using Microsoft.Agents.Builder.TestBot.Shared.Services;
using Microsoft.Agents.Builder.Testing;
using Microsoft.Agents.Builder.Testing.XUnit;
using Microsoft.Agents.Core.Models;
using Microsoft.BuilderSamples.Tests.Dialogs.TestData;
using Microsoft.BuilderSamples.Tests.Framework;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.BuilderSamples.Tests.Dialogs
{
    public class BookingDialogTests : BotTestBase
    {
        private readonly XUnitDialogTestLogger[] _middlewares;

        public BookingDialogTests(ITestOutputHelper output)
            : base(output)
        {
            _middlewares = new[] { new XUnitDialogTestLogger(output) };
        }

        [Theory]
        [MemberData(nameof(BookingDialogTestsDataGenerator.BookingFlows), MemberType = typeof(BookingDialogTestsDataGenerator))]
        public async Task DialogFlowUseCases(TestDataObject testData)
        {
            // Arrange
            var bookingTestData = testData.GetObject<BookingDialogTestCase>();
            var mockFlightBookingService = new Mock<IFlightBookingService>();
            mockFlightBookingService
                .Setup(x => x.BookFlight(It.IsAny<BookingDetails>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(bookingTestData.FlightBookingServiceResult));

            var mockGetBookingDetailsDialog = SimpleMockFactory.CreateMockDialog<GetBookingDetailsDialog>(bookingTestData.GetBookingDetailsDialogResult).Object;

            var sut = new BookingDialog(mockGetBookingDetailsDialog, mockFlightBookingService.Object);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: _middlewares);

            // Act/Assert
            Output.WriteLine($"Test Case: {bookingTestData.Name}");
            for (var i = 0; i < bookingTestData.UtterancesAndReplies.GetLength(0); i++)
            {
                var utterance = bookingTestData.UtterancesAndReplies[i, 0];

                // Send the activity and receive the first reply or just pull the next
                // activity from the queue if there's nothing to send
                var reply = !string.IsNullOrEmpty(utterance)
                    ? await testClient.SendActivityAsync<Activity>(utterance)
                    : testClient.GetNextReply<Activity>();

                Assert.Equal(bookingTestData.UtterancesAndReplies[i, 1], reply.Text);
            }

            Assert.Equal(bookingTestData.ExpectedDialogResult.Status, testClient.DialogTurnResult.Status);
        }
    }
}
