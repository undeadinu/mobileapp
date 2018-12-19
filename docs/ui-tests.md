### How to run UI tests for Android

There's only one dependency: having an actual already built and signed APK.
It doesn't matter if the APK is from a debug, release or adhoc build, it just have to be signed.

#### How to sign
For release builds, our Bitrise process uses a step to sign it, but I don't think we should run UI tests on release builds. (Except by the Google-related tests, if we ever do them)
For adhoc and debug builds, any keystore would do and we can safely use the default android debug keystore for UI tests, which can be found on a secret folder inside your home folder if you have the SDK installed (either by having Android Studio, Xamarin, or just the Android SDK installed in your machine)

For adhoc and release builds, you can grab them from the latest builds on Bitrise.
For debug builds, you'll need to archive a debug build.

Save this:
- Path: `~/.android/debug.keystore`
- Alias: `androiddebugkey`
- Store password: `android`
- Key password: `android`

On Visual Studio:
1. Select the Toggl.Giskard > Options > Android Build > Uncheck `Use Shared Mono Runtime`; 
2. Select from Visual Studio top menu: Build > Archive for publishing (this takes a while);
3. Now you have a new apk at `Toggl/bin/Debug/com.toggl.giskard.debug.apk`
4. You will have a new screen open, but you can get there by going to Build > ViewArchives;
5. Select the apk you just archived > Click in the bottom of the page "Sign and Distribute" > Ad Hoc > androiddebugkey (create it if you don't have it) > Next > Publish > Select a folder
5.1 If you don't have the androiddebugkey ready, click on "Import an Existing Key" and fill in the inputs with the values above. You can create a new one if you want, it doesn't matter much.
5.1.1 When creating a new signing key, you can choose the name, store password and key password as you wish. 
6. You have the signed debug APK (be careful, you want the apk without the -Signed suffix, it doesn't make sense... but the -Signed apk seems to have the mono runtime)! (or adhoc, the same process applies for it)

#### How to run the tests
First go to Toggl.Giskard.Tests.UI/Configuration.cs and update the path in `ApkFile` to point to your signed apk.
Ex: 
- if you put the apkName.apk file in your Desktop (MacOS) -> `.ApkFile("/Users/yourusername/Desktop/apkName.apk")`
- if you put the apk on (Repo root folder)/bin/Release/apkName.apk -> `.ApkFile("../../bin/Release/apkName.apk")`

##### To run on Visual Studio:
Have the build target to Giskard | Debug | Any CPU
On the right sidebar > Unit Tests > Giskard > Toggl.Giskard.Tests.UI
On Test Apps select the device where you want to run the tests (it can be an actual device or an emulator, but it has to be already running)
If you don't see the tests there yet, double click on the Giskard > Toggl.Giskard.Tests.UI; fix whatever bugs might be there (the build might be broken) > click again > You'll eventually see all the tests from Shared.Toggl.Tests.UI;
On Toggl.Tests.UI you can run all the tests or only the ones you want.
That's it. 

##### To run on Rider:
1st thing you should know, switching back and forth from VS and Rider might break your build. Build the project again from the command line usually fixes it; if it doesn't, clean then build again. :see_no_evil:
First run this step from the instructions on how to run the tests on VS, in VS: 
> If you don't see the tests there yet, double click on the Giskard > Toggl.Giskard.Tests.UI
In rider you just navigate in the bottom bar "Unit Tests" are and add the tests you want to run or go directly to the test classes and run the tests from the left panel icon; You can also run all the tests in the file by clicking on it and selecting "Run Unit Tests";

Remember: you'll be modifying the test files, you don't need a new apk for each test run; you can keep using the same apk while updating the tests.