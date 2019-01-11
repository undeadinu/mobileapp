# Localization

Toggl uses the built-in mechanism for localization using `Resources.resx` file. The technique is described at [Xamarin's Localization](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/app-fundamentals/localization/text?tabs=macos)

When dealing with UI strings, we have to follow the process described below:

## ☀️ Daneel

### 💻 For programatically built views

- Setup the UI's text using `System.Resources` namespace normally. 

### 📐 or Interface Builder (xib, storyboard) views 

- Create the view using IB file.
- Created outlets for the UI elements that need to be translated.

_Note: The string in the IB will be replaced, so it's better to make it something "placeholdery" obvious so we can catch the mistake early._

- Setup the text of those outlets in `ViewDidLoad` or `AwakeFromNib`

## 🤖 Giskard

Giskard is a little bit easier to deal with, since it can look up for `Resources.resx` strings directly in the `.xaml`

```xml
<TextView
    android:id="@+id/OopsTextView"
    android:textSize="13sp"
    android:textAllCaps="true"
/>
```