﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Agents.Builder.Dialogs.Prompts;
using Microsoft.Agents.Builder.Dialogs.Choices;
using Microsoft.Agents.Core.Models;
using static Microsoft.Agents.Builder.Dialogs.Prompts.PromptCultureModels;
using Microsoft.Recognizers.Text;
using Xunit;
using Microsoft.Agents.Storage;
using Microsoft.Agents.Builder.Testing;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Builder.Compat;

namespace Microsoft.Agents.Builder.Dialogs.Tests
{
    [Trait("TestCategory", "Prompts")]
    [Trait("TestCategory", "Choice Tests")]
    public class ChoicePromptTests
    {
        private readonly List<Choice> _colorChoices = new List<Choice>
        {
            new Choice { Value = "red" },
            new Choice { Value = "green" },
            new Choice { Value = "blue" },
        };

        /// <summary>
        /// Generates an Enumerable of variations on all supported locales.
        /// </summary>
        /// <returns>An iterable collection of objects.</returns>
        public static IEnumerable<object[]> GetLocaleVariationTest()
        {
            var testLocales = new TestLocale[]
            {
                new TestLocale(Bulgarian),
                new TestLocale(Chinese),
                new TestLocale(Dutch),
                new TestLocale(English),
                new TestLocale(French),
                new TestLocale(German),
                new TestLocale(Hindi),
                new TestLocale(Italian),
                new TestLocale(Japanese),
                new TestLocale(Korean),
                new TestLocale(Portuguese),
                new TestLocale(Spanish),
                new TestLocale(Swedish),
                new TestLocale(Turkish),
            };

            foreach (var locale in testLocales)
            {
                yield return new object[] { locale.ValidLocale, locale.InlineOr, locale.InlineOrMore, locale.Separator };
                yield return new object[] { locale.CapEnding, locale.InlineOr, locale.InlineOrMore, locale.Separator };
                yield return new object[] { locale.TitleEnding, locale.InlineOr, locale.InlineOrMore, locale.Separator };
                yield return new object[] { locale.CapTwoLetter, locale.InlineOr, locale.InlineOrMore, locale.Separator };
                yield return new object[] { locale.LowerTwoLetter, locale.InlineOr, locale.InlineOrMore, locale.Separator };
            }
        }

        [Fact]
        public void ChoicePromptWithEmptyIdShouldFail()
        {
            Assert.Throws<ArgumentNullException>(() => new ChoicePrompt(string.Empty));
        }

        [Fact]
        public void ChoicePromptWithNullIdShouldFail()
        {
            Assert.Throws<ArgumentNullException>(() => new ChoicePrompt(null));
        }

        [Fact]
        public async Task ChoicePromptWithCardActionAndNoValueShouldNotFail()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English));

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        var choice = new Choice()
                        {
                            Action = new CardAction()
                            {
                                Type = "imBack",
                                Value = "value",
                                Title = "title",
                            },
                        };

                        var options = new PromptOptions()
                        {
                            Choices = new List<Choice> { choice },
                        };
                        await dc.PromptAsync(
                        "ChoicePrompt",
                        options,
                        cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(StartsWithValidator(" (1) title"))
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English));

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(StartsWithValidator("favorite color?"))
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPromptAsAnInlineList()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English));

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply("favorite color? (1) red, (2) green, or (3) blue")
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPromptAsANumberedList()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create ChoicePrompt and change style to ListStyle.List which affects how choices are presented.
            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.List,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply("favorite color?\n\n   1. red\n   2. green\n   3. blue")
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPromptUsingSuggestedActions()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.SuggestedAction,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(SuggestedActionsValidator(
                    "favorite color?",
                    new SuggestedActions
                    {
                        Actions = new List<CardAction>
                        {
                            new CardAction { Type = "imBack", Value = "red", Title = "red" },
                            new CardAction { Type = "imBack", Value = "green", Title = "green" },
                            new CardAction { Type = "imBack", Value = "blue", Title = "blue" },
                        },
                    }))
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPromptUsingHeroCard()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.HeroCard,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(HeroCardValidator(
                    new HeroCard
                    {
                        Text = "favorite color?",
                        Buttons = new List<CardAction>
                        {
                            new CardAction { Type = "imBack", Value = "red", Title = "red" },
                            new CardAction { Type = "imBack", Value = "green", Title = "green" },
                            new CardAction { Type = "imBack", Value = "blue", Title = "blue" },
                        },
                    },
                    0))
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPromptUsingAppendedHeroCard()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.HeroCard,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        // Create mock attachment for testing.
                        var attachment = new Attachment { Content = "some content", ContentType = "text/plain" };

                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?", Attachments = new List<Attachment> { attachment } },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(HeroCardValidator(
                    new HeroCard
                    {
                        Text = "favorite color?",
                        Buttons = new List<CardAction>
                        {
                            new CardAction { Type = "imBack", Value = "red", Title = "red" },
                            new CardAction { Type = "imBack", Value = "green", Title = "green" },
                            new CardAction { Type = "imBack", Value = "blue", Title = "blue" },
                        },
                    },
                    1))
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPromptWithoutAddingAList()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.None,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply("favorite color?")
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldSendPromptWithoutAddingAListButAddingSsml()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.None,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity
                                {
                                    Type = ActivityTypes.Message,
                                    Text = "favorite color?",
                                    Speak = "spoken prompt",
                                },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(SpeakValidator("favorite color?", "spoken prompt"))
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldRecognizeAChoice()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.None,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                    else if (results.Status == DialogTurnStatus.Complete)
                    {
                        var choiceResult = (FoundChoice)results.Result;
                        await turnContext.SendActivityAsync(MessageFactory.Text($"{choiceResult.Value}"), cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(StartsWithValidator("favorite color?"))
                .Send("red")
                .AssertReply("red")
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldNotRecognizeOtherText()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var listPrompt = new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English)
            {
                Style = ListStyle.None,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                RetryPrompt = new Activity { Type = ActivityTypes.Message, Text = "your favorite color, please?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(StartsWithValidator("favorite color?"))
                .Send("what was that?")
                .AssertReply("your favorite color, please?")
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldCallCustomValidator()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));


            PromptValidator<FoundChoice> validator = async (promptContext, cancellationToken) =>
            {
                await promptContext.Context.SendActivityAsync(MessageFactory.Text("validator called"), cancellationToken);
                return true;
            };
            var listPrompt = new ChoicePrompt("ChoicePrompt", validator, Culture.English)
            {
                Style = ListStyle.None,
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(listPrompt);

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(StartsWithValidator("favorite color?"))
                .Send("I'll take the red please.")
                .AssertReply("validator called")
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldUseChoiceStyleIfPresent()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
                {
                    await convoState.LoadAsync(turnContext, false, cancellationToken);
                    var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                    var dialogs = new DialogSet(dialogState);
                    dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: Culture.English) { Style = ListStyle.HeroCard });

                    var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var results = await dc.ContinueDialogAsync(cancellationToken);
                    if (results.Status == DialogTurnStatus.Empty)
                    {
                        await dc.PromptAsync(
                            "ChoicePrompt",
                            new PromptOptions
                            {
                                Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?" },
                                Choices = _colorChoices,
                                Style = ListStyle.SuggestedAction,
                            },
                            cancellationToken);
                    }
                })
                .Send("hello")
                .AssertReply(SuggestedActionsValidator(
                    "favorite color?",
                    new SuggestedActions
                    {
                        Actions = new List<CardAction>
                        {
                            new CardAction { Type = "imBack", Value = "red", Title = "red" },
                            new CardAction { Type = "imBack", Value = "green", Title = "green" },
                            new CardAction { Type = "imBack", Value = "blue", Title = "blue" },
                        },
                    }))
                .StartTestAsync();
        }

        [Theory]
        [MemberData(nameof(GetLocaleVariationTest), DisableDiscoveryEnumeration = true)]
        public async Task ShouldRecognizeLocaleVariationsOfCorrectLocales(string testCulture, string inlineOr, string inlineOrMore, string separator)
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var helloLocale = MessageFactory.Text("hello");
            helloLocale.Locale = testCulture;

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await convoState.LoadAsync(turnContext, false, cancellationToken);
                var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                var dialogs = new DialogSet(dialogState);
                dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: testCulture));

                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dc.PromptAsync(
                        "ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?", Locale = testCulture },
                            Choices = _colorChoices,
                        },
                        cancellationToken);
                }
            })
                .Send(helloLocale)
                .AssertReply((activity) =>
                {
                    // Use ChoiceFactory to build the expected answer, manually
                    var expectedChoices = ChoiceFactory.Inline(_colorChoices, null, null, new ChoiceFactoryOptions()
                    {
                        InlineOr = inlineOr,
                        InlineOrMore = inlineOrMore,
                        InlineSeparator = separator,
                    }).Text;
                    Assert.Equal($"favorite color?{expectedChoices}", activity.Text);
                })
                .StartTestAsync();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-supported")]
        public async Task ShouldDefaultToEnglishLocale(string activityLocale)
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var helloLocale = MessageFactory.Text("hello");
            helloLocale.Locale = activityLocale;

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await convoState.LoadAsync(turnContext, false, cancellationToken);
                var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                var dialogs = new DialogSet(dialogState);
                dialogs.Add(new ChoicePrompt("ChoicePrompt", defaultLocale: activityLocale));

                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dc.PromptAsync(
                        "ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?", Locale = activityLocale },
                            Choices = _colorChoices,
                        },
                        cancellationToken);
                }
            })
                .Send(helloLocale)
                .AssertReply((activity) =>
                {
                    // Use ChoiceFactory to build the expected answer, manually
                    var expectedChoices = ChoiceFactory.Inline(_colorChoices, null, null, new ChoiceFactoryOptions()
                    {
                        InlineOr = English.InlineOr,
                        InlineOrMore = English.InlineOrMore,
                        InlineSeparator = English.Separator,
                    }).Text;
                    Assert.Equal($"favorite color?{expectedChoices}", activity.Text);
                })
                .StartTestAsync();
        }

        [Fact]
        public async Task ShouldAcceptAndRecognizeCustomLocaleDict()
        {
            var convoState = new ConversationState(new MemoryStorage());

            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            var culture = new PromptCultureModel()
            {
                InlineOr = " customOr ",
                InlineOrMore = " customOrMore ",
                Locale = "custom-custom",
                Separator = "customSeparator",
                NoInLanguage = "customNo",
                YesInLanguage = "customYes",
            };

            var customDict = new Dictionary<string, ChoiceFactoryOptions>()
            {
                { culture.Locale, new ChoiceFactoryOptions(culture.Separator, culture.InlineOr, culture.InlineOrMore, true) },
            };

            var helloLocale = MessageFactory.Text("hello");
            helloLocale.Locale = culture.Locale;

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await convoState.LoadAsync(turnContext, false, cancellationToken);
                var dialogState = convoState.GetValue<DialogState>("DialogState", () => new DialogState());
                var dialogs = new DialogSet(dialogState);
                dialogs.Add(new ChoicePrompt("ChoicePrompt", customDict, null, culture.Locale));

                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dc.PromptAsync(
                        "ChoicePrompt",
                        new PromptOptions
                        {
                            Prompt = new Activity { Type = ActivityTypes.Message, Text = "favorite color?", Locale = culture.Locale },
                            Choices = _colorChoices,
                        },
                        cancellationToken);
                }
            })
                .Send(helloLocale)
                .AssertReply((activity) =>
                {
                    // Use ChoiceFactory to build the expected answer, manually
                    var expectedChoices = ChoiceFactory.Inline(_colorChoices, null, null, new ChoiceFactoryOptions()
                    {
                        InlineOr = culture.InlineOr,
                        InlineOrMore = culture.InlineOrMore,
                        InlineSeparator = culture.Separator,
                    }).Text;
                    Assert.Equal($"favorite color?{expectedChoices}", activity.Text);
                })
                .StartTestAsync();
        }

        /*
        [Fact]
        public async Task ShouldHandleAnUndefinedRequest()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var testProperty = convoState.CreateProperty<Dictionary<string, object>>("test");

            var adapter = new TestAdapter()
                .Use(convoState);

            PromptValidator<FoundChoice> validator = (context, promptContext, cancellationToken) =>
            {
                Assert.IsTrue(false);
                return Task.CompletedTask;
            };

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var state = await testProperty.GetAsync(turnContext, () => new Dictionary<string, object>());
                var prompt = new ChoicePrompt(Culture.English, validator);
                prompt.Style = ListStyle.None;

                var dialogCompletion = await prompt.ContinueDialogAsync(turnContext, state);
                if (!dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                {
                    await prompt.BeginAsync(turnContext, state,
                        new ChoicePromptOptions
                        {
                            PromptString = "favorite color?",
                            Choices = ChoiceFactory.ToChoices(colorChoices)
                        });
                }
                else if (dialogCompletion.IsActive && !dialogCompletion.IsCompleted)
                {
                    if (dialogCompletion.Result == null)
                    {
                        await turnContext.SendActivityAsync("NotRecognized");
                    }
                }
            })
            .Send("hello")
            .AssertReply(StartsWithValidator("favorite color?"))
            .Send("value shouldn't have been recognized.")
            .AssertReply("NotRecognized")
            .StartTestAsync();
        }*/

        private Action<IActivity> StartsWithValidator(string expected)
        {
            return activity =>
            {
                Assert.StartsWith(expected, activity.Text);
            };
        }

        private Action<IActivity> SuggestedActionsValidator(string expectedText, SuggestedActions expectedSuggestedActions)
        {
            return activity =>
            {
                Assert.Equal(expectedText, activity.Text);
                Assert.Equal(expectedSuggestedActions.Actions.Count, activity.SuggestedActions.Actions.Count);
                for (var i = 0; i < expectedSuggestedActions.Actions.Count; i++)
                {
                    Assert.Equal(expectedSuggestedActions.Actions[i].Type, activity.SuggestedActions.Actions[i].Type);
                    Assert.Equal(expectedSuggestedActions.Actions[i].Value, activity.SuggestedActions.Actions[i].Value);
                    Assert.Equal(expectedSuggestedActions.Actions[i].Title, activity.SuggestedActions.Actions[i].Title);
                }
            };
        }

        private Action<IActivity> HeroCardValidator(HeroCard expectedHeroCard, int index)
        {
            return activity =>
            {
                var attachedHeroCard = (HeroCard)activity.Attachments[index].Content;

                Assert.Equal(expectedHeroCard.Title, attachedHeroCard.Title);
                Assert.Equal(expectedHeroCard.Buttons.Count, attachedHeroCard.Buttons.Count);
                for (var i = 0; i < expectedHeroCard.Buttons.Count; i++)
                {
                    Assert.Equal(expectedHeroCard.Buttons[i].Type, attachedHeroCard.Buttons[i].Type);
                    Assert.Equal(expectedHeroCard.Buttons[i].Value, attachedHeroCard.Buttons[i].Value);
                    Assert.Equal(expectedHeroCard.Buttons[i].Title, attachedHeroCard.Buttons[i].Title);
                }
            };
        }

        private Action<IActivity> SpeakValidator(string expectedText, string expectedSpeak)
        {
            return activity =>
            {
                Assert.Equal(expectedText, activity.Text);
                Assert.Equal(expectedSpeak, activity.Speak);
            };
        }
    }
}
