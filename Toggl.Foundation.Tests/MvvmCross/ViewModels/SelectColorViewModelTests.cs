using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MvvmCross.UI;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Xunit;
using System.Reactive.Linq;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.Services;
using Toggl.Foundation.Tests.Generators;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectColorViewModelTests
    {
        public abstract class SelectColorViewModelTest : BaseViewModelTests<SelectColorViewModel>
        {
            protected override SelectColorViewModel CreateViewModel()
                => new SelectColorViewModel(NavigationService, RxActionFactory);
        }

        public sealed class TheConstructor : SelectColorViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var navigationService = useNavigationService ? NavigationService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectColorViewModel(navigationService, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheSelectColorAction : SelectColorViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ChangesTheSelectedColor()
            {
                var initiallySelectedColor = Color.DefaultProjectColors.First();
                var colorToSelect = Color.DefaultProjectColors.Last();
                var parameters = ColorParameters.Create(initiallySelectedColor, true);

                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                ViewModel.SelectColor.Execute(colorToSelect);

                observer.Messages
                    .Select( m => m.Value.Value)
                    .Last()
                    .Single(c => c.Selected).Color.ARGB.Should().Be(colorToSelect.ARGB);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedColorIfCustomColorsAreNotAllowed()
            {
                var initiallySelectedColor = Color.DefaultProjectColors.First();
                var colorToSelect = Color.DefaultProjectColors.Last();
                var parameters = ColorParameters.Create(initiallySelectedColor, false);

                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                ViewModel.SelectColor.Execute(colorToSelect);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<MvxColor>(c => c.ARGB == colorToSelect.ARGB));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotReturnIfCustomColorsAreAllowed()
            {
                var initiallySelectedColor = Color.DefaultProjectColors.First();
                var colorToSelect = Color.DefaultProjectColors.Last();
                var parameters = ColorParameters.Create(initiallySelectedColor, true);

                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                ViewModel.SelectColor.Execute(colorToSelect);

                await NavigationService.DidNotReceive()
                    .Close(Arg.Is(ViewModel), Arg.Any<MvxColor>());
            }
        }

        public sealed class ThePrepareCommand : SelectColorViewModelTest
        {

            [Fact, LogIfTooSlow]
            public void AddsFourteenItemsToTheListOfSelectableColorsIfTheUserIsNotPro()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, false);

                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                observer.Messages
                    .Select( m => m.Value.Value)
                    .Last()
                    .Should().HaveCount(14);
            }

            [Fact, LogIfTooSlow]
            public void AddsFifteenItemsToTheListOfSelectableColorsIfTheUserIsPro()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, true);

                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                observer.Messages
                    .Select( m => m.Value.Value)
                    .Last()
                    .Should().HaveCount(15);
            }

            [Fact, LogIfTooSlow]
            public void SelectsTheColorPassedAsTheParameter()
            {
                var passedColor = Color.DefaultProjectColors.Skip(3).First();
                var parameters = ColorParameters.Create(passedColor, false);

                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                observer.Messages
                    .Select( m => m.Value.Value)
                    .Last()
                    .Single(c => c.Selected).Color.ARGB.Should().Be(passedColor.ARGB);
            }

            [Fact, LogIfTooSlow]
            public void SelectsTheFirstColorIfThePassedColorIsNotPartOfTheDefaultColorsAndWorkspaceIsNotPro()
            {
                var expected = Color.DefaultProjectColors.First();
                var parameters = ColorParameters.Create(MvxColors.Azure, false);

                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                observer.Messages
                    .Select( m => m.Value.Value)
                    .Last()
                    .Single(c => c.Selected).Color.ARGB.Should().Be(expected.ARGB);
            }

            [Fact, LogIfTooSlow]
            public void SelectsThePassedColorIfThePassedColorIsNotPartOfTheDefaultColorsAndWorkspaceIsPro()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, true);
                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableColorViewModel>>();
                ViewModel.SelectableColors.Subscribe(observer);

                ViewModel.Prepare(parameters);

                observer.Messages
                    .Select( m => m.Value.Value)
                    .Last()
                    .Single(c => c.Selected).Color.ARGB.Should().Be(MvxColors.Azure.ARGB);
            }
        }

        public class TheCloseCommand : SelectColorViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                ViewModel.Close.Execute();
                TestScheduler.Start();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<MvxColor>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheDefaultParameter()
            {
                var color = Color.DefaultProjectColors.Last();
                var parameters = ColorParameters.Create(color, true);
                ViewModel.Prepare(parameters);

                ViewModel.Close.Execute();
                TestScheduler.Start();

                NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Is(color)).Wait();
            }
        }

        public sealed class TheSaveCommand : SelectColorViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                ViewModel.Close.Execute();
                TestScheduler.Start();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<MvxColor>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedColor()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, true);
                ViewModel.Prepare(parameters);
                var expected = Color.DefaultProjectColors.First();
                ViewModel.SelectColor.Execute(expected);

                ViewModel.Save.Execute();
                TestScheduler.Start();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<MvxColor>(c => c.ARGB == expected.ARGB));
            }
        }
    }
}
