﻿using KSP.Game;
using KSP.Logging;
using KSP.Messages;
using UnityEngine.InputSystem;
using UnityEngine;
using KSP.Modding;
using KSP.IO;
using System.Collections.Generic;
using System;
using KSP.Input;
using System.Reflection;
using KSP.Sim.impl;

namespace Codenade.Inputbinder
{
    public sealed class Inputbinder : KerbalMonoBehaviour
    {
        public InputActionManager ActionManager => _actionManager;
        public KSP2Mod Mod => _mod;
        public BindingUI BindingUI => _bindingUI;
        public static Inputbinder Instance => _instance;

        private static Inputbinder _instance;
        private KSP2Mod _mod;
        private VesselComponent _vessel;
        private InputActionManager _actionManager;
        private AppBarButton _button;
        private BindingUI _bindingUI;

        public Inputbinder()
        {
            if (_instance is null)
                _instance = this;
            else
                Destroy(this);
        }

        private void OnGameStateEntered(MessageCenterMessage message)
        {
            GameStateEnteredMessage gameStateEnteredMessage = message as GameStateEnteredMessage;
            if (gameStateEnteredMessage.StateBeingEntered == GameState.MainMenu)
            {
                Initialize();
                Game.Messages.Unsubscribe<GameStateEnteredMessage>(OnGameStateEntered);
            }
        }

        private void Initialize()
        {
            RemoveKSPsGamepadBindings();
            _actionManager = InputActionManager.LoadFromJson(IOProvider.JoinPath(_mod.ModRootPath, "input.json"));
            if (_actionManager.Actions.Count == 0)
            {
                var action = new InputAction(Constants.ActionThrottleID);
                action.AddBinding("")
                    .WithName("binding");
                action.expectedControlType = "Axis";
                _actionManager.Add(action, "Throttle Axis");
                action = new InputAction(Constants.ActionPitchTrimID);
                action.AddCompositeBinding("1DAxis")
                    .With("negative", "")
                    .With("positive", "");
                action.expectedControlType = "Axis";
                _actionManager.Add(action, "Pitch Trim");
                action = new InputAction(Constants.ActionRollTrimID);
                action.AddCompositeBinding("1DAxis")
                    .With("negative", "")
                    .With("positive", "");
                action.expectedControlType = "Axis";
                _actionManager.Add(action, "Roll Trim");
                action = new InputAction(Constants.ActionYawTrimID);
                action.AddCompositeBinding("1DAxis")
                    .With("negative", "")
                    .With("positive", "");
                action.expectedControlType = "Axis";
                _actionManager.Add(action, "Yaw Trim");
                action = new InputAction(Constants.ActionTrimResetID);
                action.AddBinding("")
                    .WithName("binding");
                action.expectedControlType = "Button";
                _actionManager.Add(action, "Reset Trim");
            }
            var gameActionsToAdd = new List<InputAction>()
            {
                Game.Input.Flight.ThrottleDelta,
                Game.Input.Flight.ThrottleCutoff,
                Game.Input.Flight.ThrottleMax,
                Game.Input.Flight.Pitch,
                Game.Input.Flight.Roll,
                Game.Input.Flight.Yaw,
                Game.Input.Flight.TogglePrecisionMode,
                Game.Input.Flight.WheelSteer,
                Game.Input.Flight.WheelBrakes,
                Game.Input.Flight.WheelThrottle,
                Game.Input.Flight.Stage,
                Game.Input.Flight.ToggleLandingGear,
                Game.Input.Flight.ToggleLights,
                Game.Input.Flight.ToggleSAS,
                Game.Input.Flight.ToggleRCS,
                Game.Input.Flight.TranslateX,
                Game.Input.Flight.TranslateY,
                Game.Input.Flight.TranslateZ,
                Game.Input.Flight.TriggerActionGroup1,
                Game.Input.Flight.TriggerActionGroup2,
                Game.Input.Flight.TriggerActionGroup3,
                Game.Input.Flight.TriggerActionGroup4,
                Game.Input.Flight.TriggerActionGroup5,
                Game.Input.Flight.TriggerActionGroup6,
                Game.Input.Flight.TriggerActionGroup7,
                Game.Input.Flight.TriggerActionGroup8,
                Game.Input.Flight.TriggerActionGroup9,
                Game.Input.Flight.TriggerActionGroup10,
                Game.Input.Flight.CameraPitchGamepad,
                Game.Input.Flight.CameraYawGamepad,
                Game.Input.Flight.ShowMap,
                Game.Input.Global.ConfirmDialogue,
                Game.Input.Global.MenuGoBack,
                Game.Input.Global.QuickSave,
                Game.Input.Global.TimeWarpDecrease,
                Game.Input.Global.TimeWarpIncrease,
                Game.Input.Global.TimeWarpStop,
                Game.Input.Global.TogglePauseMenu,
                Game.Input.Global.ToggleUIVisibility
            };
            foreach (var gameAction in gameActionsToAdd)
            {
                if (_actionManager.Actions.ContainsKey(gameAction.name))
                    continue;
                _actionManager.Add(gameAction, true);
            }
            _actionManager.Actions[Constants.ActionThrottleID].Action.performed += ctx => SetThrottle(ctx.ReadValue<float>());
            _actionManager.Actions[Constants.ActionThrottleID].Action.started += ctx => SetThrottle(ctx.ReadValue<float>());
            _actionManager.Actions[Constants.ActionThrottleID].Action.canceled += ctx => SetThrottle(ctx.ReadValue<float>());
            _actionManager.Actions[Constants.ActionTrimResetID].Action.performed += ctx => ResetTrim();
            _actionManager.Actions[Constants.ActionThrottleID].Action.Enable();
            _actionManager.Actions[Constants.ActionPitchTrimID].Action.Enable();
            _actionManager.Actions[Constants.ActionRollTrimID].Action.Enable();
            _actionManager.Actions[Constants.ActionYawTrimID].Action.Enable();
            _actionManager.Actions[Constants.ActionTrimResetID].Action.Enable();
            _bindingUI = gameObject.AddComponent<BindingUI>();
            _bindingUI.Hide();
            _bindingUI.VisibilityChanged += OnUiVisibilityChange;
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
            foreach (var mod in Game.KSP2ModManager.CurrentMods)
                if (mod.ModName == Constants.Name && mod.ModAuthor == Constants.Author)
                    _mod = mod;
            StopKSPFromRemovingGamepads();
            RemoveKSPsGamepadBindings();
            InputSystem.RegisterProcessor<Processors.MapProcessor>("Map");
            InputSystem.settings.defaultDeadzoneMin = 0f;
            Game.Messages.Subscribe<GameStateEnteredMessage>(OnGameStateEntered);
        }

        private void OnUiVisibilityChange(bool visible)
        {
            if (_button?.Created is object)
                _button.State = visible;
        }

        public void SetThrottle(float value)
        {
            _vessel?.ApplyFlightCtrlState(new KSP.Sim.State.FlightCtrlStateIncremental() { mainThrottle = Mathf.Clamp01(value) });
        }

        public void ResetTrim()
        {
            _vessel?.ApplyFlightCtrlState(new KSP.Sim.State.FlightCtrlStateIncremental()
            {
                pitchTrim = 0,
                rollTrim = 0,
                yawTrim = 0
            });
        }

        private void Update()
        {
            _vessel?.ApplyFlightCtrlState(new KSP.Sim.State.FlightCtrlStateIncremental()
            {
                pitchTrim = _vessel.flightCtrlState.pitchTrim + _actionManager.Actions[Constants.ActionPitchTrimID].Action.ReadValue<float>() * Time.deltaTime,
                rollTrim = _vessel.flightCtrlState.rollTrim + _actionManager.Actions[Constants.ActionRollTrimID].Action.ReadValue<float>() * Time.deltaTime,
                yawTrim = _vessel.flightCtrlState.yawTrim + _actionManager.Actions[Constants.ActionYawTrimID].Action.ReadValue<float>() * Time.deltaTime
            });
        }

        private void StopKSPFromRemovingGamepads()
        {
            GlobalLog.Log(LogFilter.UserMod, $"[{Constants.Name}] Stopping KSP from automatically removing Gamepads...");
            var eventInfo = typeof(InputSystem).GetEvent(nameof(InputSystem.onDeviceChange), BindingFlags.Static | BindingFlags.Public);
            var method = typeof(InputManager).GetMethod("RemoveGamepadCallback", BindingFlags.NonPublic | BindingFlags.Instance);
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, Game.InputManager, method);
            eventInfo.RemoveEventHandler(null, handler);
        }

        public void RemoveKSPsGamepadBindings()
        {
            GlobalLog.Log(LogFilter.UserMod, $"[{Constants.Name}] Removing KSP's Gamepad bindings...");
            //var gamepads = Gamepad.all.ToArray();
            foreach (var action in Game.Input)
            {
                for (var i = 0; i < action.bindings.Count; i++)
                {
                    var bdg = action.bindings[i];
                    //if (!bdg.isComposite && !bdg.effectivePath.IsNullOrEmpty() && !bdg.effectivePath.Contains("Keyboard") && !bdg.effectivePath.Contains("Mouse") && !bdg.effectivePath.Contains("Gamepad"))
                    //    GlobalLog.Log(LogFilter.UserMod, $"[{Constants.Name}] LOOK {action.name} {bdg.effectivePath}");
                    if (bdg.effectivePath.Contains("Gamepad") || bdg.effectivePath.Contains("XInputController"))
                    {
                        GlobalLog.Log(LogFilter.UserMod, $"[{Constants.Name}] Hit {action.name} {bdg.effectivePath}");
                        action.ChangeBinding(i).WithPath("");
                        action.ApplyBindingOverride(i, "");
                    }
                }
            }
        }

        private void OnEnable()
        {
            GameManager.Instance.Game.Messages.Subscribe<VesselChangingMessage>(VehicleStateChanged);
            GameManager.Instance.Game.Messages.Subscribe<VesselChangedMessage>(VehicleStateChanged);
        }

        private void OnDisable()
        {
            GameManager.Instance.Game.Messages.Unsubscribe<VesselChangingMessage>(VehicleStateChanged);
            GameManager.Instance.Game.Messages.Unsubscribe<VesselChangedMessage>(VehicleStateChanged);
        }

        private void VehicleStateChanged(MessageCenterMessage msg)
        {
            _vessel = Game.ViewController.GetActiveSimVessel();
            if (_vessel is object)
            {
                if (!_bindingUI.IsInitialized && !_bindingUI.IsInitializing)
                    _bindingUI.Initialize(Game.UI.GetPopupCanvas().transform);
                if (_button is null)
                {
                    Game.Assets.LoadRaw<Sprite>(Constants.AppBarIconAssetKey).Completed += op =>
                    {
                        _button.Icon = op.Result;
                    };
                    _button = new AppBarButton($"BTN-{Constants.ID}", Constants.Name, OnAppBarButtonClicked);
                    _button.State = _bindingUI.enabled;
                }
            }
            else
            {
                _button.Dispose();
                _button = null;
                _bindingUI.Hide();
            }
        }

        private void OnAppBarButtonClicked(bool state)
        {
            _bindingUI.enabled = state;
        }
    }
}
