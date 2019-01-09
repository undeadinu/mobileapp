## Writing tests
We are using the Xamarin.UITest test framework for UI testing, you can check the documentation here: https://docs.microsoft.com/en-us/appcenter/test-cloud/uitest/
It should cover the basic api.
Keep in mind that we want checks to be cross platform when possible and reasonable. We want to avoid adding dependencies on the platforms, and when that's necessary, hide the details behind extension methods. The section [Creating assertions and extensions with slightly different behaviours on iOS and Android](#create-xplat-extensions) can help with that.

### Where do tests files go?
UI tests reside in Toggl.Tests.UI

### How to add test class
Create a file containing the test class, anotated with `[TestFixture]`, something like

```cs
[TestFixture]
public sealed class AmazingScreenTests
{
    
}
```

### How to add individual tests
Inside the test class, all you have to do is to add a new method returning void, anotated with `[Test]`, something like:

```cs
[TestFixture]
public sealed class AmazingScreenTests
{
    
    [Test]
    public void SomeActionResultsInSomething()
    {
    }   
}
```

The method name should be meaningful and usually follows the format Action -> ExpectedBehaviour

### Ignoring Tests for one platform <a name="create-xplat-extensions"></a>
Sometimes you might want or have to write tests for one platform only to avoid breaking UI tests or simply because you are testing a platform specific feature.
You can add the `[IgnoreOnAndroid]` or `[IgnoreOnIos]` annotations to ignore some specific test (method) or a group of tests (class) in Android and iOS respectively.
Ex:

```cs
[TestFixture]
[IgnoreOnAndroid]
public sealed class AmazingIOSOnlyScreenTests
{
    ... many tests inside
}

[TestFixture]
public sealed class AmazingSharedScreenTests
{
    [Test, IgnoreOnAndroid]
    public void SomeIosOnlyBehaviour()
    {
    }
    
    [Test, IgnoreOnIos]
    public void SomeAndroidOnlyBehaviour()
    {
    }    
}
``` 

In the example above, the following will happen:
- The tests inside `AmazingIOSOnlyScreenTests` will only run on iOS
- The test `SomeIosOnlyBehaviour` inside `AmazingSharedScreenTests` will only run on iOS
- The test `SomeAndroidOnlyBehaviour` inside `AmazingSharedScreenTests` will only run on Android

### Creating assertions and extensions with slightly different behaviours on iOS and Android
Extensions are usually platform independent and located at `Toggl.Tests.UI/Extensions`, but sometimes you'll need to write an extension that behaves differently on iOS and Android. 
The suggested way of writing extensions for that case is to create one extension in Android and another in iOS.
All you have to do is to add the same extension in Android and iOS, but with different implementations.
You'll add the Android version on: `Toggl.Giskard.Tests.UI/Extensions` and the iOS version on `Toggl.Daneel.Tests.UI/Extensions`.
Ex:

For iOS, in `Toggl.Daneel.Tests.UI/Extensions`:

```cs
namespace Toggl.Tests.UI.Extensions
{
    public static partial class AmazingExtensions
    {
        public static void WaitForSomethingThatBehavesDifferentlyOnEachPlat(this IApp app)
        {
            //Check iOS widget or something else iOS specific
        }
    }
}
```

For Android, in `Toggl.Giskard.Tests.UI/Extensions`:

```cs
namespace Toggl.Tests.UI.Extensions
{
    public static partial class AmazingExtensions
    {
        public static void WaitForSomethingThatBehavesDifferentlyOnEachPlat(this IApp app)
        {
            //Check Android widget or something else Android specific
        }
    }
}
```

Platform agnostic, in `Toggl.Tests.UI/Extensions`:

```cs
namespace Toggl.Tests.UI.Extensions
{
    public static partial class AmazingExtensions
    {
        public static void WaitForSomethingElseCool(this IApp app)
        {
            app.WaitForElement("AccessibilityId");
            app.Tap("SomeWidget");
            app.WaitForElement("OtherAccessibilityId");
        }
    }
}
```

Usage in tests from `Shared.Toggl.Tests.UI`:

```cs
[TestFixture]
public sealed class AmazingScreenTests
{
    
    [Test]
    public void SomeActionResultsInSomething()
    {
        app.WaitForSomethingThatBehavesDifferentlyOnEachPlat();
        app.WaitForSomethingElseCool();
    }
}
```

And of course, you can use `WaitForSomethingThatBehavesDifferentlyOnEachPlat` inside other extensions.

### Avoid breaking the ui build in the other platform
When writing new tests, if you are doing so in only one platform, remember to add whatever constants you add to `Toggl.Daneel.Tests.UI/IosAccessibilityNames.cs` to `Toggl.Giskard.Tests.UI/AndroidAccessibilityNames.cs` and vice versa.
The tests will break in the other platform if you forget to do so. 