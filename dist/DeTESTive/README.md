# DeTESTive - Unity Modding Utility for Automated Testing in Shadows of Doubt

A BepInEx Bleeding Edge (v667) plugin for Shadows of Doubt

## What is it?

DeTESTive is a modding utility designed to streamline and enhance the process of testing mod code in Shadows of Doubt. This plugin is specifically developed for modders, offering an automated end-to-end testing framework that can significantly improve productivity, reduce bugs, and help catch regressions in mod functionality caused by game updates.

## Installation

If you are not using r2modman or Thunderstore for installation, follow these steps:

1. Download BepInEx (build artifact 667 or higher) from the official repository.
2. Extract the downloaded files into the same folder as the "Shadows of Doubt.exe" executable.
3. Launch the game, load the main menu, and then exit the game.
4. Download the DeTESTive mod from the Releases page. Unzip the files and place them in corresponding directories within "Shadows of Doubt\BepInEx...".
5. Start the game. To see DeTESTive work, run the TestRunner from your own test plugin.

## Usage

To use DeTESTive and write automated tests for your mod code, follow these steps:

1.  Create a separate plugin for your tests.
2.  (Optional) Organize your tests into individual files for clarity and organization.
3.  Define your test parameters, including source, description, time scale, save file name, setup action, assertion function, failure hint, and teardown action using the Fluent API (`TestRunner.AddTest(CreateDeTest...)`, see the code block below).
4.  Add your tests to the TestRunner class.
5.  Call `TestRunner.RunTests()` to execute the defined tests.

```cs
TestRunner.AddTest(
    CreateDeTest
        .WithDescription("What this test is for")
        .WithTimescale(5.0f)
        .WhichLoadsSaveFileByPath(MyPluginInfo.PLUGIN_NAME, "TestSave.sodb")
        .WithSetup(ExampleSetup)
        .WhichAsserts(ExampleAssertion)
        .WithFailureHint("May be due to a problem with ...")
        .DefaultTeardown(),
    MyPluginInfo.PLUGIN_NAME // Sets the source for this test to the BepInEx plugin name
);
```

[You can also reference the example plugin's code.](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/DeTESTiveExample/)

### Features

DeTESTive offers several key features to assist modders in creating effective and reliable tests for their mods:

- **Fluent API for Test Definition:** DeTESTive provides a user-friendly Fluent API that allows modders to define tests in a clear and intuitive manner. This makes it easy to create comprehensive and effective tests for mod code.

- **Modular Test Writing:** Modders can write tests for their plugins in a separate plugin. These tests are defined using the Fluent API and added to the TestRunner class. The tests can be organized in their own files, ensuring a clean and organized testing environment.

- **Automated Game Save Handling:** When running tests that involve game saves, the TestRunner seamlessly navigates through the game's main menu and loads the specified save file. The save file can either be located in the game's save folder or included alongside local plugin installations.

- **Ordering and Grouping:** Tests are ordered first by their source and then by their save file. Tests with the same source and save file are grouped together and run consecutively. The order of tests with the same source and save file is deliberately shuffled to identify any potential order dependencies.

- **Optimizations for Efficiency:** DeTESTive incorporates various optimizations to enhance testing efficiency. For example, tests avoid unnecessary save reloads, can adjust the time scale, and optimize rendering to minimize draw calls during testing.

- **Comprehensive Test Reporting:** Test results are reported in the BepInEx console, providing immediate feedback to modders. Additionally, a detailed log is generated, and results are saved in an HTML file located at `<steam path>/Shadows of Doubt/DeTESTive/report.html`.

- **Flexible Assertion Functions:** Modders can utilize any assertion framework that raises exceptions upon failure, such as FluentAssertions. Assertion functions are called within a try-catch block, ensuring that exceptions result in failed tests.

### Limitations

- Unlike setup and teardown actions, assertion functions cannot be asynchronous (they cannot be coroutines). However, you can simulate similar functionality by checking conditions during setup and forwarding the results to the assertion function. See the self-contained test code from DeTESTiveExample for more information.
- You must use the built-in `TestHelper.EndGameAndReturnToMainMenu()` static method in a custom teardown function to force a reload of a save file between tests which share the same source and save file.
- You must manually refresh the test report webpage between runs to see the latest results.

## License

DeTESTive is distributed under the [MIT License](https://bitbucket.org/shadows-of-doubt-mods/mods/src/main/LICENSE). Feel free to use, modify, and distribute this modding utility as needed.