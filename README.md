[![Build Status](https://travis-ci.org/achimmihca/PrimeInputActions.svg?branch=main)](https://travis-ci.org/achimmihca/PrimeInputActions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/achimmihca/PrimeInputActions/blob/main/LICENSE)
[![Sponsor this project](https://img.shields.io/badge/-Sponsor-fafbfc?logo=GitHub%20Sponsors)](https://github.com/sponsors/achimmihca)

# PrimeInputActions
Augment Unity's InputActions with

- Priority queue and cancellation
- UniRx Observable
- Handy C# constants

# Why PrimeInputActions

## Using UniRx Observables
PrimeInputActions wraps the Unity events using UniRx.
This way, events can be composed and filtered using UniRx's reactive API.

Example (modified from [UniRx](https://github.com/neuecc/UniRx#introduction)):
```
var clickStream = InputManager.GetInputAction("ui/click").PerformedAsObservable();
clickStream.Buffer(clickStream.Throttle(TimeSpan.FromMilliseconds(250)))
    .Where(xs => xs.Count >= 2)
    .Subscribe(xs => Debug.Log("Double click detected! Count:" + xs.Count));
```

## Solving conflicts
Using Unity's InputActions (v1.0.2) can easily lead to two kind of conflicts:
- (a) Same InputAction is used to do different things.
    - Example: "cancel" action (e.g. Escape key) is used to close a popup dialog and to exit the scene.
- (b) One InputAction is a subset of another
    - Example: "Shift+Ctrl+S" of "save as" action would also trigger "Ctrl+S" of "save" action

PrimeInputActions solves (a) by using a priority queue and cancellation (see code below).
- In the above example, listening to close the dialog could be done with higher priority and cancel further notification.

(b) Can be solved by checking that the more complex action is not triggered when listening for the simpler action.

## Remove Bindings on Destroy
- The InputManager will remove all subscriptions to InputActions when its OnDestroy method is called.
    - This is typically the case when changing scenes.
- Thus, subscriptions to InputActions do not need to be disposed manually when they are used throughout a scene.
- To opt-out from this, you can set the `useDontDestroyOnLoad` flag of the InputManager.

# How to Use

- This package builds on Unity's new [input system](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html) and the [UniRx](https://github.com/neuecc/UniRx) library.
- Both have to be present in your project already.

## Get the Package
- You can add a dependency to your `Packages/manifest.json` using a [Git URL](https://docs.unity3d.com/2019.4/Documentation/Manual/upm-git.html) in the following form:
  `"com.achimmihca.primeinputactions": "https://github.com/achimmihca/PrimeInputActions.git?path=PrimeInputActions/Packages/com.achimmihca.primeinputactions#v1.0.0"`
  - Note that `#v1.0.0` can be used to specify a tag or commit.
- This package ships with a sample that can be imported to your project using Unity's Package Manager.

## Prepare a Scene
- Add a tag "InputManager"
- Create an empty GameObject and add the `InputManager` component to it
- In the InputManager's inspector, set the defaultInputActionAsset
- You can now call `InputManager.GetInputAction`

## (Optional) Generate Constants
- Use the corresponding menu item to create C# constants for your InputActions.
    - Using constants instead of strings enables auto-completion, avoids typos, and makes refactoring easier.
    - Example: `InputManager.GetInputAction(R.InputActions.ui_cancel)`
- The InputManager has a flag `generateConstantsOnResourceChange`. When set to true, then saving the defaultInputActionAsset will also trigger the generation of constants.
- The path where the generated code will be saved is also specified in the InputManager.

## (Optional) Copy InputActionAsset to Application.persistentDataPath
- The InputManager has a flag to specify whether the InputActionAsset should be copied to [Application.persistentDataPath](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html).
- If set to true, then the Asset will be saved to this location if it does not exist there yet.
- Furthermore, the InputActionAsset will be loaded from this location if possible.

This enables to persist changed InputActions at runtime. Users could modify bindings to their preferences.

## Subscribe to InputActions
Simple exmaple:
```
InputManager.GetInputAction("ui/cancel")
    .PerformedAsObservable()
    .Subscribe(ctx => Debug.Log("'cancel' triggered"))
```

Example with priority and cancellation:
```
InputManager.GetInputAction("ui/cancel")
    .PerformedAsObservable(10 /* Optionally, specify a priority. Higher priority is notified first. */)
    .Subscribe(ctx => 
    {
        // Subscribers with lower priority should not be notified in this frame.
        InputManager.GetInputAction("ui/cancel").CancelNotifyForThisFrame(); 
        Debug.Log("'cancel' triggered");
    })
```

# History
PrimeInputActions has been created originally for [UltraStar Play](https://github.com/UltraStar-Deluxe/Play).
If you like singing, karaoke, or sing-along games then go check it out ;)
